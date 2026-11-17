
using Microsoft.EntityFrameworkCore;
using PartnerCRM.Models;

namespace PartnerCRM.Data
{
    public static class DbInitializer
    {
        public static async Task InitializeAsync(
            ApplicationDbContext context)
        {
            await context.Database.MigrateAsync();

            // Státy
            var countries = new List<Country>
            {
                new Country
                {
                    Name = "Česká republika",
                    Code2 = "CZ",
                    Code3 = "CZE"
                },
                new Country
                {
                    Name = "Slovensko",
                    Code2 = "SK",
                    Code3 = "SVK"
                },
                new Country
                {
                    Name = "Německo",
                    Code2 = "DE",
                    Code3 = "DEU"
                },
                new Country
                {
                    Name = "Rakousko",
                    Code2 = "AT",
                    Code3 = "AUT"
                },
                new Country
                {
                    Name = "Polsko",
                    Code2 = "PL",
                    Code3 = "POL"
                }
            };

            // Zrušení případného dosavadního výchozího státu
            var existingCountries =
                await context.Countries.ToListAsync();

            foreach (var country in existingCountries)
            {
                country.IsDefault = false;
            }

            // Doplnění chybějících států podle ISO kódu
            foreach (var country in countries)
            {
                if (!existingCountries.Any(c =>
                    c.Code2 == country.Code2))
                {
                    context.Countries.Add(country);
                }
            }

            await context.SaveChangesAsync();

            // Nastavení České republiky jako výchozího státu
            var defaultCountry =
                await context.Countries.FirstOrDefaultAsync(
                    c => c.Code2 == "CZ");

            if (defaultCountry != null)
            {
                defaultCountry.IsDefault = true;
            }

            // Banky
            var banks = new List<Bank>
            {
                new Bank { Name = "Česká spořitelna", BankCode = "0800" },
                new Bank { Name = "ČSOB", BankCode = "0300" },
                new Bank { Name = "Komerční banka", BankCode = "0100" },
                new Bank { Name = "MONETA Money Bank", BankCode = "0600" },
                new Bank { Name = "Fio banka", BankCode = "2010" },
                new Bank { Name = "Raiffeisenbank", BankCode = "5500" },
                new Bank { Name = "UniCredit Bank", BankCode = "2700" },
                new Bank { Name = "Air Bank", BankCode = "3030" },
                new Bank { Name = "mBank", BankCode = "6210" },
                new Bank { Name = "Banka CREDITAS", BankCode = "2250" }
            };

            // Doplnění chybějících bank podle bankovního kódu
            foreach (var bank in banks)
            {
                if (!await context.Banks.AnyAsync(
                    b => b.BankCode == bank.BankCode))
                {
                    context.Banks.Add(bank);
                }
            }



            // Typy firem
            var companyTypes = new List<string>
            {
                "Zákazník",
                "Dodavatel",
                "Vývojový partner",
                "Průmyslový partner",
                "Integrátor",
                "Investor",
                "Konkurence",
                "Škola",
                "Instituce"
            };

            // Doplnění chybějících typů firem
            foreach (var typeName in companyTypes)
            {
                if (!await context.CompanyTypes.AnyAsync(
                    t => t.Name == typeName))
                {
                    context.CompanyTypes.Add(new CompanyType
                    {
                        Name = typeName
                    });
                }
            }


            // Krajská města České republiky

            var czechRepublic = await context.Countries
                .FirstOrDefaultAsync(c => c.Code2 == "CZ");

            if (czechRepublic != null)
            {
                var cities = new List<(string Name, string PostalCode)>
    {
        ("Praha", "11000"),
        ("České Budějovice", "37001"),
        ("Plzeň", "30100"),
        ("Karlovy Vary", "36001"),
        ("Ústí nad Labem", "40001"),
        ("Liberec", "46001"),
        ("Hradec Králové", "50002"),
        ("Pardubice", "53002"),
        ("Jihlava", "58601"),
        ("Brno", "60200"),
        ("Olomouc", "77900"),
        ("Ostrava", "70200"),
        ("Zlín", "76001")
    };

                foreach (var city in cities)
                {
                    if (!await context.Cities.AnyAsync(c =>
                        c.Name == city.Name &&
                        c.PostalCode == city.PostalCode &&
                        c.CountryId == czechRepublic.Id))
                    {
                        context.Cities.Add(new City
                        {
                            Name = city.Name,
                            PostalCode = city.PostalCode,
                            CountryId = czechRepublic.Id
                        });
                    }
                }
            }


            await context.SaveChangesAsync();
        }
    }
}
