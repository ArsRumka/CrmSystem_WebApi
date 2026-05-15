namespace CrmSystem.ApiTests.Infrastructure;

public static class TestData
{
    public static string UniqueSuffix(string? label = null)
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        return string.IsNullOrWhiteSpace(label)
            ? suffix
            : $"{Sanitize(label)}-{suffix}";
    }

    public static object Client(
        string? email = null,
        string? phone = null,
        bool allowMarketingEmails = true,
        string? suffix = null)
    {
        var value = UniqueSuffix(suffix);

        return new
        {
            firstName = $"First{value}",
            lastName = $"Last{value}",
            middleName = (string?)null,
            email = email ?? $"client-{value}@example.test",
            phone,
            status = 2,
            source = 3,
            allowMarketingEmails,
            notes = "Created by API integration test"
        };
    }

    public static object Category(string? name = null)
    {
        return new
        {
            name = name ?? $"Category {UniqueSuffix()}",
            parentCategoryId = (Guid?)null,
            bonusType = 0,
            bonusValue = (decimal?)null,
            discountType = 3,
            discountValue = (decimal?)null
        };
    }

    public static object Product(Guid? categoryId = null, decimal price = 100m, string? name = null)
    {
        var suffix = UniqueSuffix();

        return new
        {
            categoryId,
            name = name ?? $"Product {suffix}",
            sku = $"SKU-{suffix}",
            description = "API test product",
            price,
            bonusType = 0,
            bonusValue = (decimal?)null,
            discountType = 3,
            discountValue = (decimal?)null
        };
    }

    public static object Service(Guid? categoryId = null, decimal price = 50m, string? name = null)
    {
        return new
        {
            categoryId,
            name = name ?? $"Service {UniqueSuffix()}",
            description = "API test service",
            price,
            bonusType = 0,
            bonusValue = (decimal?)null,
            discountType = 3,
            discountValue = (decimal?)null
        };
    }

    public static object Storage(string? name = null)
    {
        return new
        {
            name = name ?? $"Storage {UniqueSuffix()}",
            address = "Test address",
            isDefault = true
        };
    }

    public static object BonusSettings(
        decimal pointValue = 1m,
        decimal accrualValue = 10m,
        decimal maxPaymentPercent = 50m)
    {
        return new
        {
            isEnabled = true,
            pointValue,
            accrualType = 1,
            accrualValue,
            maxPaymentPercent,
            accrueOnBonusPayment = true
        };
    }

    public static object EmailSettings(string? password = null)
    {
        return new
        {
            senderName = "CRM Tests",
            senderEmail = "sender@example.test",
            smtpHost = "smtp.example.test",
            smtpPort = 587,
            useSsl = true,
            username = "sender@example.test",
            smtpPassword = password ?? "secret-password",
            isEnabled = true
        };
    }

    public static object EmailTemplate(string? name = null)
    {
        return new
        {
            name = name ?? $"Template {UniqueSuffix()}",
            subject = "Hello {{FirstName}}",
            body = "Dear {{FullName}}, welcome back to {{OrganizationName}}",
            isHtml = false
        };
    }

    private static string Sanitize(string label)
    {
        return new string(label
            .Where(char.IsLetterOrDigit)
            .Select(char.ToLowerInvariant)
            .ToArray());
    }
}
