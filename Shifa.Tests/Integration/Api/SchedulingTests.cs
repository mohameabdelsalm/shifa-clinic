using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using Shifa.Tests.Integration.Infrastructure;

namespace Shifa.Tests.Integration.Api;

/// <summary>
/// Phase 5: availability, booking without double booking, idempotency, the appointment lifecycle, branch scope,
/// tenant isolation and portal booking (ADR-008). The app runs on a fake clock that starts on
/// Sunday 6 January 2030, 08:00 in Cairo (+02:00); the doctor works Mondays 09:00–13:00 on a 15-minute grid
/// and the service takes 30 minutes (two slots).
/// </summary>
[Collection(SqlServerCollection.Name)]
public sealed class SchedulingTests
{
    private const string Slug = "al-noor";
    private const string Parent = "+201012345678";
    private static readonly DateTimeOffset Sunday0800 = new(2030, 1, 6, 6, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Monday = new(2030, 1, 7);

    private readonly SqlServerFixture _db;

    public SchedulingTests(SqlServerFixture db)
    {
        _db = db;
    }

    private sealed class Clinic
    {
        public required ApiTestHost Api { get; init; }
        public required FakeTimeProvider Clock { get; init; }
        public required long TenantId { get; init; }
        public required long BranchId { get; init; }
        public required long DoctorId { get; init; }
        public required long ServiceId { get; init; }
        public required long SpecialtyId { get; init; }
        public string Admin { get; set; } = "";

        /// <summary>Moves the clock and signs the admin in again (access tokens live 15 minutes on the same clock).</summary>
        public async Task MoveClockToAsync(DateTimeOffset utc)
        {
            Clock.SetUtcNow(utc);
            Admin = await Api.AdminTokenAsync(Slug);
        }
    }

    private async Task<Clinic> ClinicAsync(string slug = Slug, ApiTestHost? existing = null, FakeTimeProvider? clock = null)
    {
        clock ??= new FakeTimeProvider(Sunday0800);
        var api = existing ?? await ApiTestHost.StartAsync(_db, clock);
        var platform = await api.PlatformTokenAsync();
        var tenantId = await api.CreateTenantAsync(platform, await api.CreatePlanAsync(platform, $"PLAN-{slug}"), slug);
        var admin = await api.AdminTokenAsync(slug);

        var branchId = (await api.PostAsync("/api/branches", admin, new { name = "Nasr City", code = "NASR" })).Data.GetInt64();
        var specialtyId = (await api.PostAsync("/api/specialties", admin, new { nameEn = "Cardiology", nameAr = "قلب" })).Data.GetInt64();
        var doctor = await api.PostAsync("/api/doctors", admin, new
        {
            fullNameEn = "Dr. Synthetic",
            fullNameAr = "د. تجريبي",
            specialtyIds = new[] { specialtyId },
            branchIds = new[] { branchId },
        });
        Assert.Equal(HttpStatusCode.OK, doctor.Status);
        var serviceId = (await api.PostAsync("/api/services", admin, new { specialtyId, nameEn = "Consultation", nameAr = "كشف", durationMinutes = 30 })).Data.GetInt64();

        var clinic = new Clinic
        {
            Api = api, Clock = clock, TenantId = tenantId, BranchId = branchId, DoctorId = doctor.Data.GetInt64(),
            ServiceId = serviceId, SpecialtyId = specialtyId, Admin = admin,
        };
        // Booking needs a current price for the service at the branch (ADR-011).
        await PriceAsync(clinic, branchId);
        Assert.Equal(HttpStatusCode.OK, (await SetScheduleAsync(clinic, branchId, "Monday")).Status);
        return clinic;
    }

    private static async Task PriceAsync(Clinic c, long branchId) =>
        Assert.Equal(HttpStatusCode.OK, (await c.Api.PutAsync("/api/services/price", c.Admin, new { serviceId = c.ServiceId, branchId, price = 300 })).Status);

    private static Task<ApiResult> SetScheduleAsync(Clinic c, long branchId, params string[] days) =>
        c.Api.PutAsync("/api/doctors/schedule", c.Admin, new
        {
            doctorId = c.DoctorId,
            branchId,
            items = days.Select(d => new { dayOfWeek = d, startTime = "09:00:00", endTime = "13:00:00", slotMinutes = 15 }).ToArray(),
        });

    private static async Task<long> PatientAsync(Clinic c, string name)
    {
        var result = await c.Api.PostAsync("/api/patients", c.Admin, new { fullName = name, dateOfBirth = "1990-05-01", gender = "Female" });
        Assert.Equal(HttpStatusCode.OK, result.Status);
        return result.Data.GetInt64();
    }

    private static string Cairo(DateOnly day, string time) => $"{day:yyyy-MM-dd}T{time}:00+02:00";

    private static DateTimeOffset CairoInstant(DateOnly day, string time) => DateTimeOffset.Parse(Cairo(day, time));

    private static Task<ApiResult> BookAsync(Clinic c, long patientId, string localTime, string? key = null, DateOnly? day = null,
        long? branchId = null, string? token = null) =>
        c.Api.CallAsync(HttpMethod.Post, "/api/appointments", token ?? c.Admin,
            new
            {
                patientId,
                branchId = branchId ?? c.BranchId,
                doctorId = c.DoctorId,
                serviceId = c.ServiceId,
                startAt = Cairo(day ?? Monday, localTime),
                source = "Reception",
            },
            key is null ? null : new Dictionary<string, string> { ["Idempotency-Key"] = key });

    private static async Task<long> BookedAsync(Clinic c, long patientId, string localTime, DateOnly? day = null)
    {
        var result = await BookAsync(c, patientId, localTime, Guid.NewGuid().ToString(), day);
        Assert.Equal(HttpStatusCode.OK, result.Status);
        return result.Data.GetInt64();
    }

    private static async Task<List<string>> AvailableAsync(Clinic c, DateOnly? day = null, long? branchId = null, string? token = null)
    {
        var result = await c.Api.PostAsync("/api/appointments/availability", token ?? c.Admin,
            new { branchId = branchId ?? c.BranchId, doctorId = c.DoctorId, serviceId = c.ServiceId, date = (day ?? Monday).ToString("yyyy-MM-dd") });
        Assert.Equal(HttpStatusCode.OK, result.Status);
        return result.Data.EnumerateArray().Select(s => s.GetProperty("localStart").GetDateTime().ToString("HH:mm")).ToList();
    }

    private static async Task<JsonElement> ViewAsync(Clinic c, long id)
    {
        var result = await c.Api.GetAsync($"/api/appointments/{id}", c.Admin);
        Assert.Equal(HttpStatusCode.OK, result.Status);
        return result.Data;
    }

    private async Task<int> LiveTicksAsync(Clinic c, long? appointmentId = null)
    {
        await using var context = _db.CreateSchedulingContext(c.TenantId);
        return await context.AppointmentSlots.CountAsync(s => appointmentId == null || s.AppointmentId == appointmentId);
    }

    [Fact]
    public async Task Availability_lists_free_starts_and_booking_takes_them()
    {
        var c = await ClinicAsync();
        await using var host = c.Api;
        var patient = await PatientAsync(c, "Patient One");

        var before = await AvailableAsync(c);
        Assert.Equal(15, before.Count);                                // 09:00 … 12:30, each needing 30 minutes
        Assert.Equal("09:00", before[0]);
        Assert.Equal("12:30", before[^1]);
        Assert.Empty(await AvailableAsync(c, Monday.AddDays(1)));      // no schedule on Tuesday

        var id = await BookedAsync(c, patient, "09:00");
        var view = await ViewAsync(c, id);
        Assert.Equal("Confirmed", view.GetProperty("status").GetString());
        Assert.Equal("Reception", view.GetProperty("source").GetString());
        Assert.Equal(CairoInstant(Monday, "09:00"), view.GetProperty("startAt").GetDateTimeOffset());
        Assert.Equal(CairoInstant(Monday, "09:30"), view.GetProperty("endAt").GetDateTimeOffset());
        Assert.Equal(6, await LiveTicksAsync(c, id));                  // 30 minutes = six 5-minute ticks

        var after = await AvailableAsync(c);
        Assert.DoesNotContain("09:00", after);
        Assert.DoesNotContain("09:15", after);                         // would overlap 09:00–09:30
        Assert.Equal("09:30", after[0]);

        // A start that isn't offered is refused.
        Assert.Equal(HttpStatusCode.Conflict, (await BookAsync(c, await PatientAsync(c, "Patient Two"), "09:05", "k-off-grid")).Status);
        Assert.Equal(HttpStatusCode.Conflict, (await BookAsync(c, await PatientAsync(c, "Patient Three"), "08:00", "k-before")).Status);

        var search = await c.Api.PostAsync("/api/appointments/search", c.Admin, new { fromDate = "2030-01-07", toDate = "2030-01-07", pageNumber = 1, pageSize = 10 });
        Assert.Equal(1, search.Data.GetProperty("items").GetArrayLength());
        Assert.Equal(HttpStatusCode.BadRequest, (await c.Api.PostAsync("/api/appointments/search", c.Admin, new { fromDate = "2030-01-01", toDate = "2030-06-01" })).Status);
    }

    [Fact]
    public async Task Ten_parallel_bookings_of_one_slot_create_exactly_one_appointment()
    {
        var c = await ClinicAsync();
        await using var host = c.Api;
        var patients = new List<long>();
        for (var i = 0; i < 10; i++)
            patients.Add(await PatientAsync(c, $"Racer {i}"));

        var results = await Task.WhenAll(patients.Select(p => BookAsync(c, p, "10:00", $"race-{p}")));

        Assert.Single(results, r => r.Status == HttpStatusCode.OK);
        Assert.All(results.Where(r => r.Status != HttpStatusCode.OK), r => Assert.Equal(HttpStatusCode.Conflict, r.Status));
        Assert.Equal(6, await LiveTicksAsync(c));
        await using var context = _db.CreateSchedulingContext(c.TenantId);
        Assert.Equal(1, await context.Appointments.CountAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Overlapping_bookings_in_parallel_cannot_both_win()
    {
        var c = await ClinicAsync();
        await using var host = c.Api;
        var first = await PatientAsync(c, "First");
        var second = await PatientAsync(c, "Second");

        // 10:00–10:30 and 10:15–10:45 share the 10:15–10:30 ticks.
        var results = await Task.WhenAll(BookAsync(c, first, "10:00", "overlap-a"), BookAsync(c, second, "10:15", "overlap-b"));

        Assert.Single(results, r => r.Status == HttpStatusCode.OK);
        Assert.Single(results, r => r.Status == HttpStatusCode.Conflict);
        Assert.Equal(6, await LiveTicksAsync(c));
    }

    [Fact]
    public async Task Idempotency_key_is_required_replays_the_first_result_and_refuses_reuse()
    {
        var c = await ClinicAsync();
        await using var host = c.Api;
        var patient = await PatientAsync(c, "Patient");
        var other = await PatientAsync(c, "Other");

        Assert.Equal(HttpStatusCode.BadRequest, (await BookAsync(c, patient, "09:00", key: null)).Status);

        var first = await BookAsync(c, patient, "09:00", "key-1");
        var retry = await BookAsync(c, patient, "09:00", "key-1");
        Assert.Equal(HttpStatusCode.OK, retry.Status);
        Assert.Equal(first.Data.GetInt64(), retry.Data.GetInt64());

        var reused = await BookAsync(c, patient, "11:00", "key-1");
        Assert.Equal(HttpStatusCode.UnprocessableEntity, reused.Status);

        // The same retry sent five times at once still makes one appointment and every caller gets its id.
        var parallel = await Task.WhenAll(Enumerable.Range(0, 5).Select(_ => BookAsync(c, other, "10:00", "key-2")));
        Assert.All(parallel, r => Assert.Equal(HttpStatusCode.OK, r.Status));
        Assert.Single(parallel.Select(r => r.Data.GetInt64()).Distinct());

        await using var context = _db.CreateSchedulingContext(c.TenantId);
        Assert.Equal(2, await context.Appointments.CountAsync(TestContext.Current.CancellationToken));

        // After 24 hours the key is forgotten and can be used for a new booking.
        await c.MoveClockToAsync(Sunday0800.AddHours(25));
        var later = await BookAsync(c, patient, "12:00", "key-1", day: Monday.AddDays(7));
        Assert.Equal(HttpStatusCode.OK, later.Status);
        Assert.NotEqual(first.Data.GetInt64(), later.Data.GetInt64());
    }

    [Fact]
    public async Task Cancelling_frees_the_slot_for_someone_else()
    {
        var c = await ClinicAsync();
        await using var host = c.Api;
        var patient = await PatientAsync(c, "Patient");
        var other = await PatientAsync(c, "Other");

        var id = await BookedAsync(c, patient, "09:00");
        Assert.Equal(HttpStatusCode.OK, (await c.Api.PutAsync($"/api/appointments/{id}/cancel", c.Admin, new { reason = "Travelling" })).Status);
        Assert.Equal(HttpStatusCode.OK, (await c.Api.PutAsync($"/api/appointments/{id}/cancel", c.Admin, new { })).Status);   // repeat is a no-op

        var view = await ViewAsync(c, id);
        Assert.Equal("Cancelled", view.GetProperty("status").GetString());
        Assert.Equal("Travelling", view.GetProperty("cancelReason").GetString());
        Assert.Equal(0, await LiveTicksAsync(c, id));
        Assert.Contains("09:00", await AvailableAsync(c));

        await BookedAsync(c, other, "09:00");
        await BookedAsync(c, patient, "11:00");                         // a cancelled visit doesn't count for the one-per-day rule
        Assert.Equal(HttpStatusCode.BadRequest, (await c.Api.PutAsync($"/api/appointments/{id}/confirm", c.Admin)).Status);
    }

    [Fact]
    public async Task Reschedule_moves_onto_its_own_ticks_and_releases_the_old_ones()
    {
        var c = await ClinicAsync();
        await using var host = c.Api;
        var patient = await PatientAsync(c, "Patient");
        var other = await PatientAsync(c, "Other");

        var id = await BookedAsync(c, patient, "09:00");
        var moved = await c.Api.PutAsync("/api/appointments/reschedule", c.Admin, new { id, startAt = Cairo(Monday, "09:15") });
        Assert.Equal(HttpStatusCode.OK, moved.Status);

        var view = await ViewAsync(c, id);
        Assert.Equal(CairoInstant(Monday, "09:15"), view.GetProperty("startAt").GetDateTimeOffset());
        Assert.Equal(6, await LiveTicksAsync(c, id));

        var otherId = await BookedAsync(c, other, "09:45");
        Assert.Equal(HttpStatusCode.Conflict, (await BookAsync(c, await PatientAsync(c, "Third"), "09:00", "k-third")).Status);
        Assert.Equal(HttpStatusCode.Conflict, (await c.Api.PutAsync("/api/appointments/reschedule", c.Admin, new { id, startAt = Cairo(Monday, "09:30") })).Status);

        // Moving to another week frees Monday morning completely.
        Assert.Equal(HttpStatusCode.OK, (await c.Api.PutAsync("/api/appointments/reschedule", c.Admin, new { id, startAt = Cairo(Monday.AddDays(7), "09:00") })).Status);
        Assert.Contains("09:00", await AvailableAsync(c));
        Assert.Equal(12, await LiveTicksAsync(c));
        Assert.NotEqual(0, otherId);

        // Only Pending/Confirmed appointments can move.
        Assert.Equal(HttpStatusCode.OK, (await c.Api.PutAsync($"/api/appointments/{id}/cancel", c.Admin, new { })).Status);
        Assert.Equal(HttpStatusCode.BadRequest, (await c.Api.PutAsync("/api/appointments/reschedule", c.Admin, new { id, startAt = Cairo(Monday, "11:00") })).Status);
    }

    [Fact]
    public async Task A_patient_has_at_most_one_visit_per_doctor_per_day()
    {
        var c = await ClinicAsync();
        await using var host = c.Api;
        var patient = await PatientAsync(c, "Patient");

        await BookedAsync(c, patient, "09:00");
        Assert.Equal(HttpStatusCode.BadRequest, (await BookAsync(c, patient, "11:00", "second-visit")).Status);
        await BookedAsync(c, patient, "11:00", Monday.AddDays(7));
        Assert.Equal(HttpStatusCode.BadRequest, (await BookAsync(c, 999_999_999, "12:00", "ghost-patient")).Status);
    }

    [Fact]
    public async Task A_doctor_busy_at_one_branch_is_busy_at_every_branch()
    {
        var c = await ClinicAsync();
        await using var host = c.Api;
        var maadi = (await c.Api.PostAsync("/api/branches", c.Admin, new { name = "Maadi", code = "MAADI" })).Data.GetInt64();
        await PriceAsync(c, maadi);
        Assert.Equal(HttpStatusCode.OK, (await c.Api.PutAsync("/api/doctors", c.Admin, new
        {
            id = c.DoctorId, fullNameEn = "Dr. Synthetic", fullNameAr = "د. تجريبي", specialtyIds = new[] { c.SpecialtyId }, branchIds = new[] { c.BranchId, maadi },
        })).Status);

        await BookedAsync(c, await PatientAsync(c, "Patient"), "09:00");

        // Monday moves from Nasr to Maadi after the booking was made: the Nasr visit still blocks those minutes.
        Assert.Equal(HttpStatusCode.OK, (await SetScheduleAsync(c, c.BranchId, "Tuesday")).Status);
        Assert.Equal(HttpStatusCode.OK, (await SetScheduleAsync(c, maadi, "Monday")).Status);

        var atMaadi = await AvailableAsync(c, branchId: maadi);
        Assert.DoesNotContain("09:00", atMaadi);
        Assert.DoesNotContain("09:15", atMaadi);
        Assert.Contains("09:30", atMaadi);
        Assert.Equal(HttpStatusCode.Conflict, (await BookAsync(c, await PatientAsync(c, "Other"), "09:15", "maadi", branchId: maadi)).Status);
    }

    [Fact]
    public async Task Check_in_opens_two_hours_before_and_gives_queue_numbers_no_show_waits_thirty_minutes()
    {
        var c = await ClinicAsync();
        await using var host = c.Api;
        var p1 = await BookedAsync(c, await PatientAsync(c, "P1"), "09:00");
        var p2 = await BookedAsync(c, await PatientAsync(c, "P2"), "09:30");
        var p3 = await BookedAsync(c, await PatientAsync(c, "P3"), "10:00");
        var p4 = await BookedAsync(c, await PatientAsync(c, "P4"), "11:00");
        var p5 = await BookedAsync(c, await PatientAsync(c, "P5"), "12:30");

        Task<ApiResult> CheckIn(long id) => c.Api.PutAsync($"/api/appointments/{id}/check-in", c.Admin);
        Task<ApiResult> NoShow(long id) => c.Api.PutAsync($"/api/appointments/{id}/no-show", c.Admin);

        Assert.Equal(HttpStatusCode.BadRequest, (await CheckIn(p1)).Status);                        // the day before

        await c.MoveClockToAsync(CairoInstant(Monday, "06:55"));
        Assert.Equal(HttpStatusCode.BadRequest, (await CheckIn(p1)).Status);                        // 2h05 before

        await c.MoveClockToAsync(CairoInstant(Monday, "07:00"));
        var first = await CheckIn(p1);
        Assert.Equal(HttpStatusCode.OK, first.Status);
        Assert.Equal(1, first.Data.GetProperty("queueNumber").GetInt32());
        var invoiceId = first.Data.GetProperty("invoiceId").GetInt64();                             // check-in issues the visit invoice (ADR-011)
        var repeat = await CheckIn(p1);                                                             // repeat is a no-op
        Assert.Equal(HttpStatusCode.OK, repeat.Status);
        Assert.Equal(1, repeat.Data.GetProperty("queueNumber").GetInt32());
        Assert.Equal(invoiceId, repeat.Data.GetProperty("invoiceId").GetInt64());                   // same invoice, not a second one
        Assert.Equal(HttpStatusCode.BadRequest, (await CheckIn(p2)).Status);                        // 09:30 opens at 07:30

        await c.MoveClockToAsync(CairoInstant(Monday, "08:00"));
        Assert.Equal(2, (await CheckIn(p3)).Data.GetProperty("queueNumber").GetInt32());                                       // queue follows arrival, not booking time
        Assert.Equal(3, (await CheckIn(p2)).Data.GetProperty("queueNumber").GetInt32());
        Assert.Equal("CheckedIn", (await ViewAsync(c, p2)).GetProperty("status").GetString());
        Assert.Equal(HttpStatusCode.BadRequest, (await c.Api.PutAsync($"/api/appointments/{p2}/cancel", c.Admin, new { })).Status);
        Assert.Equal(HttpStatusCode.BadRequest, (await NoShow(p2)).Status);

        await c.MoveClockToAsync(CairoInstant(Monday, "11:29"));
        Assert.Equal(HttpStatusCode.BadRequest, (await NoShow(p4)).Status);
        await c.MoveClockToAsync(CairoInstant(Monday, "11:30"));
        Assert.Equal(HttpStatusCode.OK, (await NoShow(p4)).Status);
        Assert.Equal(HttpStatusCode.BadRequest, (await CheckIn(p4)).Status);                        // no-show is final

        await c.MoveClockToAsync(CairoInstant(Monday.AddDays(1), "00:05"));
        Assert.Equal(HttpStatusCode.BadRequest, (await CheckIn(p5)).Status);                        // the local day is over
    }

    [Fact]
    public async Task Parallel_check_ins_get_distinct_queue_numbers()
    {
        var c = await ClinicAsync();
        await using var host = c.Api;
        var ids = new List<long>();
        foreach (var (time, i) in new[] { "09:00", "09:30", "10:00", "10:30", "11:00" }.Select((t, i) => (t, i)))
            ids.Add(await BookedAsync(c, await PatientAsync(c, $"Queue {i}"), time));

        await c.MoveClockToAsync(CairoInstant(Monday, "09:00"));
        var results = await Task.WhenAll(ids.Select(id => c.Api.PutAsync($"/api/appointments/{id}/check-in", c.Admin)));

        Assert.All(results, r => Assert.Equal(HttpStatusCode.OK, r.Status));
        Assert.Equal([1, 2, 3, 4, 5], results.Select(r => r.Data.GetProperty("queueNumber").GetInt32()).Order().ToArray());
    }

    [Fact]
    public async Task Check_in_and_cancel_at_the_same_moment_have_one_winner()
    {
        var c = await ClinicAsync();
        await using var host = c.Api;
        var id = await BookedAsync(c, await PatientAsync(c, "Patient"), "09:00");
        await c.MoveClockToAsync(CairoInstant(Monday, "08:00"));

        var results = await Task.WhenAll(
            c.Api.PutAsync($"/api/appointments/{id}/check-in", c.Admin),
            c.Api.PutAsync($"/api/appointments/{id}/cancel", c.Admin, new { reason = "Race" }));

        Assert.Single(results, r => r.Status == HttpStatusCode.OK);
        Assert.Contains(results.Single(r => r.Status != HttpStatusCode.OK).Status, new[] { HttpStatusCode.Conflict, HttpStatusCode.BadRequest });

        var status = (await ViewAsync(c, id)).GetProperty("status").GetString();
        if (results[0].Status == HttpStatusCode.OK)
        {
            Assert.Equal("CheckedIn", status);
            Assert.Equal(6, await LiveTicksAsync(c, id));
        }
        else
        {
            Assert.Equal("Cancelled", status);
            Assert.Equal(0, await LiveTicksAsync(c, id));
        }
    }

    [Fact]
    public async Task Staff_limited_to_a_branch_cannot_see_or_book_at_other_branches()
    {
        var c = await ClinicAsync();
        await using var host = c.Api;
        var maadi = (await c.Api.PostAsync("/api/branches", c.Admin, new { name = "Maadi", code = "MAADI" })).Data.GetInt64();
        await PriceAsync(c, maadi);
        Assert.Equal(HttpStatusCode.OK, (await c.Api.PutAsync("/api/doctors", c.Admin, new
        {
            id = c.DoctorId, fullNameEn = "Dr. Synthetic", fullNameAr = "د. تجريبي", specialtyIds = new[] { c.SpecialtyId }, branchIds = new[] { c.BranchId, maadi },
        })).Status);
        Assert.Equal(HttpStatusCode.OK, (await SetScheduleAsync(c, maadi, "Tuesday")).Status);

        var patient = await PatientAsync(c, "Patient");
        var atNasr = await BookedAsync(c, patient, "09:00");
        var atMaadi = await BookAsync(c, patient, "09:00", "maadi-1", Monday.AddDays(1), maadi);
        Assert.Equal(HttpStatusCode.OK, atMaadi.Status);
        var maadiId = atMaadi.Data.GetInt64();

        var roleId = (await c.Api.PostAsync("/api/roles", c.Admin, new
        {
            name = "Nasr Reception",
            permissions = new[] { "Patients.Read", "Appointments.Read", "Appointments.Create", "Appointments.Cancel", "Appointments.CheckIn" },
        })).Data.GetInt64();
        Assert.Equal(HttpStatusCode.OK, (await c.Api.PostAsync("/api/staff", c.Admin, new
        {
            fullName = "Nasr Desk", email = "desk@al-noor.test", password = ApiTestHost.Password, roleIds = new[] { roleId }, allBranches = false, branchIds = new[] { c.BranchId },
        })).Status);
        var desk = await c.Api.TokenAsync("desk@al-noor.test", ApiTestHost.Password, Slug);

        Assert.Equal(HttpStatusCode.OK, (await c.Api.GetAsync($"/api/appointments/{atNasr}", desk)).Status);
        Assert.Equal(HttpStatusCode.NotFound, (await c.Api.GetAsync($"/api/appointments/{maadiId}", desk)).Status);
        Assert.Equal(HttpStatusCode.NotFound, (await c.Api.PutAsync($"/api/appointments/{maadiId}/cancel", desk, new { })).Status);
        Assert.Equal(HttpStatusCode.NotFound, (await c.Api.PutAsync("/api/appointments/reschedule", desk, new { id = maadiId, startAt = Cairo(Monday.AddDays(1), "10:00") })).Status);
        Assert.Equal(HttpStatusCode.NotFound, (await c.Api.PostAsync("/api/appointments/availability", desk,
            new { branchId = maadi, doctorId = c.DoctorId, serviceId = c.ServiceId, date = "2030-01-08" })).Status);
        Assert.Equal(HttpStatusCode.NotFound, (await BookAsync(c, await PatientAsync(c, "Other"), "10:00", "desk-maadi", Monday.AddDays(1), maadi, desk)).Status);

        var search = await c.Api.PostAsync("/api/appointments/search", desk, new { fromDate = "2030-01-07", toDate = "2030-01-08", pageNumber = 1, pageSize = 10 });
        Assert.Equal(1, search.Data.GetProperty("items").GetArrayLength());
        Assert.Equal(atNasr, search.Data.GetProperty("items")[0].GetProperty("id").GetInt64());
    }

    [Fact]
    public async Task Tenant_B_cannot_see_change_or_book_against_tenant_A()
    {
        var a = await ClinicAsync("al-noor");
        await using var host = a.Api;
        var b = await ClinicAsync("al-shifa", existing: a.Api, clock: a.Clock);
        var id = await BookedAsync(a, await PatientAsync(a, "A Patient"), "09:00");

        Assert.Equal(HttpStatusCode.NotFound, (await b.Api.GetAsync($"/api/appointments/{id}", b.Admin)).Status);
        Assert.Equal(HttpStatusCode.NotFound, (await b.Api.PutAsync($"/api/appointments/{id}/cancel", b.Admin, new { })).Status);
        Assert.Equal(HttpStatusCode.NotFound, (await b.Api.PutAsync($"/api/appointments/{id}/confirm", b.Admin)).Status);
        Assert.Equal(HttpStatusCode.NotFound, (await b.Api.PutAsync("/api/appointments/reschedule", b.Admin, new { id, startAt = Cairo(Monday, "10:00") })).Status);

        var search = await b.Api.PostAsync("/api/appointments/search", b.Admin, new { fromDate = "2030-01-07", toDate = "2030-01-07", pageNumber = 1, pageSize = 10 });
        Assert.Equal(0, search.Data.GetProperty("items").GetArrayLength());

        // A's branch, doctor and service ids mean nothing inside B.
        var foreign = await b.Api.PostAsync("/api/appointments/availability", b.Admin,
            new { branchId = a.BranchId, doctorId = a.DoctorId, serviceId = a.ServiceId, date = "2030-01-07" });
        Assert.Equal(HttpStatusCode.BadRequest, foreign.Status);
        Assert.Equal(HttpStatusCode.BadRequest, (await BookAsync(a, await PatientAsync(b, "B Patient"), "10:00", "cross", token: b.Admin)).Status);

        // B's own doctor is free at the same minute: tick uniqueness is per tenant.
        Assert.Contains("09:00", await AvailableAsync(b));
        await BookedAsync(b, await PatientAsync(b, "B Patient 2"), "09:00");
    }

    [Fact]
    public async Task Portal_users_book_and_cancel_only_for_linked_patients_within_the_rules()
    {
        var c = await ClinicAsync();
        await using var host = c.Api;
        var child = await PatientAsync(c, "Child");
        var viewOnly = await PatientAsync(c, "View Only");
        var stranger = await PatientAsync(c, "Stranger");
        Assert.Equal(HttpStatusCode.OK, (await c.Api.PostAsync("/api/patients/portal-access", c.Admin, new { patientId = child, phoneNumber = Parent, relationship = "Parent" })).Status);
        Assert.Equal(HttpStatusCode.OK, (await c.Api.PostAsync("/api/patients/portal-access", c.Admin, new { patientId = viewOnly, phoneNumber = Parent, relationship = "Parent", canBook = false })).Status);
        var strangerVisit = await BookedAsync(c, stranger, "12:00");

        Assert.Equal(HttpStatusCode.OK, (await c.Api.PostAsync("/api/portal/auth/otp/request", null, new { tenantSlug = Slug, phoneNumber = Parent })).Status);
        var verified = await c.Api.PostAsync("/api/portal/auth/otp/verify", null, new { tenantSlug = Slug, phoneNumber = Parent, code = c.Api.LastOtp(Parent) });
        var portal = verified.Data.GetProperty("accessToken").GetString()!;

        Task<ApiResult> PortalBook(long patientId, string time, string key) =>
            c.Api.CallAsync(HttpMethod.Post, "/api/portal/appointments", portal,
                new { patientId, branchId = c.BranchId, doctorId = c.DoctorId, serviceId = c.ServiceId, startAt = Cairo(Monday, time) },
                new Dictionary<string, string> { ["Idempotency-Key"] = key });

        var slots = await c.Api.PostAsync("/api/portal/appointments/availability", portal,
            new { branchId = c.BranchId, doctorId = c.DoctorId, serviceId = c.ServiceId, date = "2030-01-07" });
        Assert.Equal(HttpStatusCode.OK, slots.Status);

        var booked = await PortalBook(child, "09:00", "portal-1");
        Assert.Equal(HttpStatusCode.OK, booked.Status);
        Assert.Equal("Online", (await ViewAsync(c, booked.Data.GetInt64())).GetProperty("source").GetString());
        Assert.Equal(HttpStatusCode.NotFound, (await PortalBook(viewOnly, "10:00", "portal-2")).Status);
        Assert.Equal(HttpStatusCode.NotFound, (await PortalBook(stranger, "10:00", "portal-3")).Status);

        var mine = await c.Api.GetAsync("/api/portal/appointments", portal);
        Assert.Equal(1, mine.Data.GetArrayLength());
        Assert.Equal(HttpStatusCode.NotFound, (await c.Api.PutAsync($"/api/portal/appointments/{strangerVisit}/cancel", portal, new { })).Status);

        // Staff endpoints refuse portal tokens and portal endpoints refuse staff tokens.
        Assert.Equal(HttpStatusCode.Forbidden, (await c.Api.GetAsync($"/api/appointments/{strangerVisit}", portal)).Status);
        Assert.Equal(HttpStatusCode.Forbidden, (await c.Api.GetAsync("/api/portal/appointments", c.Admin)).Status);

        // Cancelling online closes 3 hours before the visit.
        Assert.Equal(HttpStatusCode.OK, (await c.Api.PutAsync($"/api/portal/appointments/{booked.Data.GetInt64()}/cancel", portal, new { reason = "Feeling better" })).Status);
        var again = await PortalBook(child, "11:00", "portal-4");
        Assert.Equal(HttpStatusCode.OK, again.Status);

        c.Clock.SetUtcNow(CairoInstant(Monday, "08:01"));
        Assert.Equal(HttpStatusCode.OK, (await c.Api.PostAsync("/api/portal/auth/otp/request", null, new { tenantSlug = Slug, phoneNumber = Parent })).Status);
        portal = (await c.Api.PostAsync("/api/portal/auth/otp/verify", null, new { tenantSlug = Slug, phoneNumber = Parent, code = c.Api.LastOtp(Parent) }))
            .Data.GetProperty("accessToken").GetString()!;
        var late = await c.Api.PutAsync($"/api/portal/appointments/{again.Data.GetInt64()}/cancel", portal, new { });
        Assert.Equal(HttpStatusCode.BadRequest, late.Status);
    }

    [Fact]
    public async Task Online_bookings_of_patients_with_three_no_shows_wait_for_confirmation()
    {
        var c = await ClinicAsync();
        await using var host = c.Api;
        var patient = await PatientAsync(c, "Often Absent");
        Assert.Equal(HttpStatusCode.OK, (await c.Api.PostAsync("/api/patients/portal-access", c.Admin, new { patientId = patient, phoneNumber = Parent, relationship = "Self" })).Status);

        var missed = new List<long>();
        for (var week = 0; week < 3; week++)
            missed.Add(await BookedAsync(c, patient, "09:00", Monday.AddDays(7 * week)));

        await c.MoveClockToAsync(CairoInstant(Monday.AddDays(14), "12:00"));
        foreach (var id in missed)
            Assert.Equal(HttpStatusCode.OK, (await c.Api.PutAsync($"/api/appointments/{id}/no-show", c.Admin)).Status);

        await c.Api.PostAsync("/api/portal/auth/otp/request", null, new { tenantSlug = Slug, phoneNumber = Parent });
        var portal = (await c.Api.PostAsync("/api/portal/auth/otp/verify", null, new { tenantSlug = Slug, phoneNumber = Parent, code = c.Api.LastOtp(Parent) }))
            .Data.GetProperty("accessToken").GetString()!;
        var booked = await c.Api.CallAsync(HttpMethod.Post, "/api/portal/appointments", portal,
            new { patientId = patient, branchId = c.BranchId, doctorId = c.DoctorId, serviceId = c.ServiceId, startAt = Cairo(Monday.AddDays(21), "09:00") },
            new Dictionary<string, string> { ["Idempotency-Key"] = "after-no-shows" });
        Assert.Equal(HttpStatusCode.OK, booked.Status);

        var id2 = booked.Data.GetInt64();
        Assert.Equal("Pending", (await ViewAsync(c, id2)).GetProperty("status").GetString());
        Assert.Equal(HttpStatusCode.BadRequest, (await c.Api.PutAsync($"/api/appointments/{id2}/check-in", c.Admin)).Status);
        Assert.Equal(HttpStatusCode.OK, (await c.Api.PutAsync($"/api/appointments/{id2}/confirm", c.Admin)).Status);
        Assert.Equal("Confirmed", (await ViewAsync(c, id2)).GetProperty("status").GetString());
    }
}
