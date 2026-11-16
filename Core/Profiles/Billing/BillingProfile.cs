using AutoMapper;
using Core.DTO.Billing;
using Core.Entities.Billing;

namespace Core.Profiles.Billing;

/// <summary>
/// Billing reads only: invoices, payments, refunds, credit notes and shifts are created by the repos from server-side
/// data (prices from the price list, amounts checked by the entities), never mapped from request models.
/// </summary>
public class BillingProfile : Profile
{
    public BillingProfile()
    {
        CreateMap<Invoice, InvoiceListDto>()
            .ForMember(dest => dest.Kind, opt => opt.MapFrom(src => src.Kind.ToString()))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
            .ForMember(dest => dest.Balance, opt => opt.MapFrom(src => src.Total - src.CreditedAmount - src.PaidAmount));

        CreateMap<Invoice, InvoiceDto>()
            .ForMember(dest => dest.Kind, opt => opt.MapFrom(src => src.Kind.ToString()))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
            .ForMember(dest => dest.Balance, opt => opt.MapFrom(src => src.Total - src.CreditedAmount - src.PaidAmount))
            .ForMember(dest => dest.Items, opt => opt.Ignore())
            .ForMember(dest => dest.Payments, opt => opt.Ignore())
            .ForMember(dest => dest.Refunds, opt => opt.Ignore())
            .ForMember(dest => dest.CreditNotes, opt => opt.Ignore());

        CreateMap<Invoice, PortalInvoiceDto>()
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
            .ForMember(dest => dest.Balance, opt => opt.MapFrom(src => src.Total - src.CreditedAmount - src.PaidAmount))
            .ForMember(dest => dest.Items, opt => opt.Ignore());

        CreateMap<InvoiceItem, InvoiceItemDto>();

        // Secrets are never mapped out: only whether they are set.
        CreateMap<PaymentGatewaySettings, PaymentGatewayDto>()
            .ForMember(dest => dest.IntegrationIds, opt => opt.MapFrom(src =>
                src.IntegrationIds.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(long.Parse).ToList()))
            .ForMember(dest => dest.HasSecretKey, opt => opt.MapFrom(src => src.SecretKeyProtected != ""))
            .ForMember(dest => dest.HasHmacSecret, opt => opt.MapFrom(src => src.HmacSecretProtected != ""))
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => src.ModificationDate ?? src.CreationDate));

        CreateMap<OnlinePaymentAttempt, OnlinePaymentAttemptDto>()
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreationDate));

        CreateMap<Payment, PaymentDto>()
            .ForMember(dest => dest.Method, opt => opt.MapFrom(src => src.Method.ToString()));

        CreateMap<Refund, RefundDto>()
            .ForMember(dest => dest.Method, opt => opt.MapFrom(src => src.Method.ToString()))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreationDate));

        CreateMap<CreditNote, CreditNoteDto>()
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreationDate));

        // Live totals are filled by the repo.
        CreateMap<CashierShift, CashierShiftDto>()
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
            .ForMember(dest => dest.CashIn, opt => opt.Ignore())
            .ForMember(dest => dest.CashOut, opt => opt.Ignore())
            .ForMember(dest => dest.CardIn, opt => opt.Ignore())
            .ForMember(dest => dest.WalletIn, opt => opt.Ignore())
            .ForMember(dest => dest.NonCashOut, opt => opt.Ignore())
            .ForMember(dest => dest.PaymentsCount, opt => opt.Ignore());
    }
}
