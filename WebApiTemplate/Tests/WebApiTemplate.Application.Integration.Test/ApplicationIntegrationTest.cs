using Xunit;
using WebApiTemplate.Application.Product.Interfaces;
using WebApiTemplate.Application.Product.Services;
using WebApiTemplate.Application.EmailTemplate.Interfaces;
using WebApiTemplate.Application.EmailTemplate.Services;
using WebApiTemplate.Application.Email.Interfaces;
using WebApiTemplate.Application.Email.Services;
using System.Linq;
using WebApiTemplate.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using WebApiTemplate.Domain.Enums.EmailTemplate;
using System.Collections.Generic;
using WebApiTemplate.Domain.Consts.EmailTemplate;

namespace WebApiTemplate.Application.Integration.Test
{
    public class ApplicationIntegrationTest
    {
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;

        public ApplicationIntegrationTest()
        {
            _configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.test.json")
                .AddEnvironmentVariables()
                .Build();

            _connectionString = _configuration.GetConnectionString("ConnectionString");
        }

        [Fact]
        public async void TestProducts()
        {
            var optionsBuilder = new DbContextOptionsBuilder<DatabaseContext>();

            optionsBuilder.UseSqlServer(_connectionString);

            using (var context = new DatabaseContext(optionsBuilder.Options))
            {
                IProductService productService = new ProductService(context, null, null);
                var products = await productService.Get();
                Assert.True(products != null && products.Any());
            }
        }

        [Fact]
        public async void TestEmailtemplate()
        {
            var optionsBuilder = new DbContextOptionsBuilder<DatabaseContext>();

            optionsBuilder.UseSqlServer(_connectionString);

            using (var context = new DatabaseContext(optionsBuilder.Options))
            {
                IEmailService emailService = new EmailService(_configuration);
                IEmailTemplateService emailTemplateService = new EmailTemplateService(context, emailService);

                string email = "@gmail.com";
                var paramDict = new Dictionary<string, string>
                {
                    { EmailTemplateDataChanged.DataTitle, "test" }
                };
                await emailTemplateService.SendEmailTemplate(email, EmailTemplateType.DataChanged, paramDict);
                Assert.True(true);
            }
        }
    }
}