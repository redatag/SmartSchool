namespace SmartSchool.Modules.Identity.Domain;

public static class Permissions
{
    public const string IdentityRolesManage = "identity.roles.manage";
    public const string StudentsView = "students.view";
    public const string StudentsManage = "students.manage";
    public const string AttendanceView = "attendance.view";
    public const string AttendanceRecord = "attendance.record";
    public const string FinanceInvoiceView = "finance.invoice.view";
    public const string FinanceInvoiceCreate = "finance.invoice.create";
    public const string FinancePaymentCreate = "finance.payment.create";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        IdentityRolesManage,
        StudentsView,
        StudentsManage,
        AttendanceView,
        AttendanceRecord,
        FinanceInvoiceView,
        FinanceInvoiceCreate,
        FinancePaymentCreate
    };
}
