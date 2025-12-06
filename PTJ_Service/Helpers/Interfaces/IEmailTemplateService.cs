using PTJ_Service.Helpers.Implementations;

namespace PTJ_Service.Helpers.Interfaces
{
    public interface IEmailTemplateService
    {
        string CreateVerifyEmailTemplate(string verifyLink);
        string CreateResetPasswordTemplate(string resetLink);

        string CreateApplicationAcceptedTemplate(string name, string postTitle, string employerName);
        string CreateApplicationRejectedTemplate(string name, string postTitle);
        string CreateInterviewInviteTemplate(string name, string postTitle, string employerName, string interviewNote);

        string CreateEmployerRejectedTemplate(string companyName, string reason);
        string CreateEmployerApprovedTemplate(string companyName, string verifyLink);
        string CreateGoogleEmployerApprovedTemplate(string companyName);
        string CreateGoogleEmployerRejectedTemplate(string companyName, string reason);
        string CreateEmployerPaymentSuccessTemplate(string employerName, string planName, decimal amount, int remainingPosts, DateTime startDate, DateTime? endDate);
    }

}
