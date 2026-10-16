using System;
using System.Collections.Generic;

namespace MyLifeGame.Models
{
    public class Asset
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Icon { get; set; }
        public int Price { get; set; }
        public int MonthlyIncome { get; set; }
        public int MinChapter { get; set; } = 1;
        public string Description { get; set; }
    }

    public class Npc
    {
        public string Name { get; set; }
        public string Personality { get; set; }
        public int Friendship { get; set; } = 0;
        public int MetAtMonth { get; set; }
    }

    public class PendingChain
    {
        public string Id { get; set; }
        public int TriggerAtTurn { get; set; }
        public string Description { get; set; }
    }

    public class Player
    {
        public string Name { get; set; } = "Безымянный";
        public int Age { get; set; } = 18;

        // 1 ход = 1 неделя, 4 недели = 1 месяц
        public int Week { get; set; } = 1;
        public int Month { get; set; } = 1;
        public int Year { get; set; } = 0;

        public int Health { get; set; } = 100;
        public int Money { get; set; } = 0;
        public int Happiness { get; set; } = 30;
        public int Intelligence { get; set; } = 5;
        public int Social { get; set; } = 5;
        public int Energy { get; set; } = 100;

        public string Job { get; set; } = "Бомж";
        public string Status { get; set; } = "🚮 Бомж";
        public int Chapter { get; set; } = 1;

        public int BusinessBonus { get; set; } = 0;
        public string BusinessName { get; set; } = null;

        // Семья
        public string SpouseName { get; set; } = null;
        public int SpouseHappiness { get; set; } = 50;
        public int ChildrenCount { get; set; } = 0;
        public List<string> ChildrenNames { get; set; } = new List<string>();

        // Образование
        public string University { get; set; } = null;
        public string Education { get; set; } = null;
        public int EducationLevel { get; set; } = 0;

        // Путешествия
        public int TripsCount { get; set; } = 0;
        public List<string> VisitedCountries { get; set; } = new List<string>();

        // Банк
        public int LoanAmount { get; set; } = 0;
        public int LoanRemaining { get; set; } = 0;
        public int LoanMonthlyPayment { get; set; } = 0;
        public int LoanMonthsLeft { get; set; } = 0;
        public int LoansTaken { get; set; } = 0;
        public int LoansDefaulted { get; set; } = 0;

     
        public List<string> Achievements { get; set; } = new List<string>();
        public List<string> OwnedAssets { get; set; } = new List<string>();
        public List<Npc> KnownNpcs { get; set; } = new List<Npc>();
        public List<PendingChain> PendingChains { get; set; } = new List<PendingChain>();

        public int LastTaxPaid { get; set; } = 0;
        public int TotalTaxPaid { get; set; } = 0;

        public bool IsAlive => Health > 0 && Age < 90;
        public bool HasLoan => LoanRemaining > 0;
        public bool IsBankrupt => Money < -50000;

        public string Season
        {
            get
            {
                if (Month == 12 || Month == 1 || Month == 2) return "❄ Зима";
                if (Month >= 3 && Month <= 5) return "🌸 Весна";
                if (Month >= 6 && Month <= 8) return "☀ Лето";
                return "🍂 Осень";
            }
        }

        public double EducationBonus
        {
            get
            {
                return EducationLevel switch
                {
                    1 => 1.15,
                    2 => 1.30,
                    3 => 1.50,
                    _ => 1.00
                };
            }
        }


        public int MonthlyIncome
        {
            get
            {
                int jobIncome = Job switch
                {
                    // Сюжетные работы
                    "Грузчик" => 4000,
                    "Продавец" => 6000,
                    "Владелец кофейни" => 15000,
                    "Бизнесмен" => 40000,
                    "Магнат" => 150000,

                    // Работы из случайных событий
                    "Курьер" => 5000,
                    "Официант" => 4500,
                    "Уборщик" => 3500,
                    "Сторож" => 4000,
                    "Водитель" => 7000,
                    "Строитель" => 6500,
                    "Упаковщик" => 4000,
                    "Мойщик" => 3500,

                    // Работы после вуза
                    "Программист" => 25000,
                    "Врач" => 22000,
                    "Юрист" => 28000,
                    "Инженер" => 20000,
                    "Учитель" => 12000,

                    _ => 0
                };

                jobIncome = (int)(jobIncome * EducationBonus);


                return jobIncome + GetAssetsIncome() + BusinessBonus;
            }
        }


        public int WeeklyIncome => MonthlyIncome / 4;

        public int GetAssetsIncome()
        {
            int total = 0;
            foreach (var id in OwnedAssets)
            {
                var asset = AssetCatalog.GetById(id);
                if (asset != null) total += asset.MonthlyIncome;
            }
            return total;
        }

        public int MonthlyExpenses => 0;

        public double TaxRate
        {
            get
            {
                int yearlyIncome = MonthlyIncome * 12;
                if (yearlyIncome < 10000) return 0.0;
                if (yearlyIncome < 50000) return 0.13;
                if (yearlyIncome < 200000) return 0.20;
                if (yearlyIncome < 500000) return 0.25;
                return 0.30;
            }
        }

        public string TaxRateText
        {
            get
            {
                int yearlyIncome = MonthlyIncome * 12;
                if (yearlyIncome < 10000) return "0%";
                if (yearlyIncome < 50000) return "13%";
                if (yearlyIncome < 200000) return "20%";
                if (yearlyIncome < 500000) return "25%";
                return "30%";
            }
        }

        public int CalculateYearlyTax()
        {
            int yearlyIncome = MonthlyIncome * 12;
            int tax = (int)(yearlyIncome * TaxRate);

            int deductions = ChildrenCount * 5000;
            if (HasAsset("apartment")) deductions += 5000;
            if (HasAsset("factory")) deductions += 10000;
            deductions += Intelligence * 50;

            return Math.Max(0, tax - deductions);
        }

        public event Action<Player> OnChanged;

        public void Modify(Action<Player> action)
        {
            action(this);
            Clamp();
            OnChanged?.Invoke(this);
        }

        public void Clamp()
        {
            Health = Math.Clamp(Health, 0, 100);
            Happiness = Math.Clamp(Happiness, 0, 100);
            Intelligence = Math.Clamp(Intelligence, 0, 100);
            Social = Math.Clamp(Social, 0, 100);
            Energy = Math.Clamp(Energy, 0, 100);
            SpouseHappiness = Math.Clamp(SpouseHappiness, 0, 100);
        }

        public bool HasAsset(string id) => OwnedAssets.Contains(id);
        public bool IsMarried => !string.IsNullOrEmpty(SpouseName);
        public bool HasEducation => EducationLevel > 0;

        public Npc FindNpc(string name)
        {
            foreach (var n in KnownNpcs)
                if (n.Name == name) return n;
            return null;
        }

        public void AddAchievement(string id)
        {
            if (!Achievements.Contains(id))
                Achievements.Add(id);
        }

        public string MonthName => Month switch
        {
            1 => "Январь",
            2 => "Февраль",
            3 => "Март",
            4 => "Апрель",
            5 => "Май",
            6 => "Июнь",
            7 => "Июль",
            8 => "Август",
            9 => "Сентябрь",
            10 => "Октябрь",
            11 => "Ноябрь",
            12 => "Декабрь",
            _ => "?"
        };
    }

    public static class AssetCatalog
    {
        public static List<Asset> All => new List<Asset>
        {
            new Asset { Id = "sleeping_bag", Name = "Спальник", Icon = "🛏️",
                Price = 300, MonthlyIncome = 0, MinChapter = 1, Description = "+5 ❤/мес" },
            new Asset { Id = "phone", Name = "Кнопочный телефон", Icon = "📱",
                Price = 1000, MonthlyIncome = 0, MinChapter = 1, Description = "Открывает новые события" },
            new Asset { Id = "bike", Name = "Велосипед", Icon = "🚲",
                Price = 5000, MonthlyIncome = 0, MinChapter = 2, Description = "+1 ❤/мес" },
            new Asset { Id = "deposit", Name = "Банковский вклад", Icon = "🏦",
                Price = 20000, MonthlyIncome = 800, MinChapter = 2, Description = "+800₽/мес" },
            new Asset { Id = "coffee", Name = "Кофейня", Icon = "☕",
                Price = 30000, MonthlyIncome = 3000, MinChapter = 3, Description = "+3000₽/мес" },
            new Asset { Id = "stocks", Name = "Акции", Icon = "📈",
                Price = 50000, MonthlyIncome = 5000, MinChapter = 3, Description = "+5000₽/мес" },
            new Asset { Id = "apartment", Name = "Квартира (аренда)", Icon = "🏠",
                Price = 80000, MonthlyIncome = 8000, MinChapter = 3, Description = "+8000₽/мес" },
            new Asset { Id = "shop", Name = "Магазин", Icon = "🏪",
                Price = 150000, MonthlyIncome = 15000, MinChapter = 4, Description = "+15000₽/мес" },
            new Asset { Id = "restaurant", Name = "Ресторан", Icon = "🍽️",
                Price = 300000, MonthlyIncome = 35000, MinChapter = 4, Description = "+35000₽/мес" },
            new Asset { Id = "factory", Name = "Завод", Icon = "🏭",
                Price = 700000, MonthlyIncome = 80000, MinChapter = 4, Description = "+80000₽/мес" },
            new Asset { Id = "skyscraper", Name = "Небоскрёб", Icon = "🏙️",
                Price = 2000000, MonthlyIncome = 250000, MinChapter = 5, Description = "+250000₽/мес" },
            new Asset { Id = "football_club", Name = "Футбольный клуб", Icon = "⚽",
                Price = 5000000, MonthlyIncome = 500000, MinChapter = 5, Description = "+500000₽/мес" },
            new Asset { Id = "yacht", Name = "Яхта", Icon = "🛥️",
                Price = 10000000, MonthlyIncome = 0, MinChapter = 5, Description = "Символ статуса" }
        };

        public static Asset GetById(string id)
        {
            foreach (var a in All)
                if (a.Id == id) return a;
            return null;
        }
    }
}
