using MyLifeGame.Models;

namespace MyLifeGame.Patterns.EventStrategies
{
    // ═══════════════════════════════════════════════
    //   ГЛАВА 1 — БОМЖ
    // ═══════════════════════════════════════════════

    public class BeggingStrategy : IEventStrategy
    {
        public string Name => "Попрошайничество";
        public GameEvent Generate(Player player) => new GameEvent
        {
            Title = "🙏 На паперти",
            Description = "Ты сидишь у метро. Прохожие спешат мимо.",
            Choices =
            {
                new EventChoice
                {
                    Text = "🙏 Просить целый день  →  +80-200₽,  -5 😊",
                    ResultMessage = "Кто-то бросил монету. Унизительно, но деньги есть.",
                    Effect = p => p.Modify(x =>
                    {
                        x.Money += new System.Random().Next(80, 200);
                        x.Happiness -= 5;
                    })
                },
                new EventChoice
                {
                    Text = "😴 Посидеть спокойно  →  +10 ❤,  +5 😊",
                    ResultMessage = "Ты отдохнул на солнце.",
                    Effect = p => p.Modify(x => { x.Health += 10; x.Happiness += 5; })
                }
            }
        };
    }

    public class BottleStrategy : IEventStrategy
    {
        public string Name => "Сбор бутылок";
        public GameEvent Generate(Player player) => new GameEvent
        {
            Title = "🍾 Сбор бутылок",
            Description = "Ты нашёл богатый район — там много бутылок.",
            Choices =
            {
                new EventChoice
                {
                    Text = "💪 Собрать всё  →  +400₽,  -15 ❤",
                    ResultMessage = "Целая сумка бутылок! Спина болит.",
                    Effect = p => p.Modify(x => { x.Money += 400; x.Health -= 15; })
                },
                new EventChoice
                {
                    Text = "🚶 Собрать чуть-чуть  →  +150₽",
                    ResultMessage = "Немного, но легко.",
                    Effect = p => p.Modify(x => x.Money += 150)
                }
            }
        };
    }

    public class ShelterStrategy : IEventStrategy
    {
        public string Name => "Ночлег";
        public GameEvent Generate(Player player) => new GameEvent
        {
            Title = "🌙 Холодная ночь",
            Description = "На улице -5°C. Где будешь ночевать?",
            Choices =
            {
                new EventChoice
                {
                    Text = "🏚️ В подвале (бесплатно)  →  -12 ❤,  -5 😊",
                    ResultMessage = "Холодно, но бесплатно.",
                    Effect = p => p.Modify(x => { x.Health -= 12; x.Happiness -= 5; })
                },
                new EventChoice
                {
                    Text = "🛏️ Хостел  →  -200₽,  +20 ❤,  +10 😊",
                    ResultMessage = "Выспался в тепле.",
                    Effect = p => p.Modify(x =>
                    {
                        x.Money -= 200;
                        x.Health += 20;
                        x.Happiness += 10;
                    }),
                    IsAvailable = p => p.Money >= 200,
                    NotAvailableReason = "❌ Нужно 200₽"
                }
            }
        };
    }

    // ═══════════════════════════════════════════════
    //   ГЛАВА 2 — РАБОТА
    // ═══════════════════════════════════════════════

    public class JobSearchStrategy : IEventStrategy
    {
        public string Name => "Поиск работы";
        public GameEvent Generate(Player player) => new GameEvent
        {
            Title = "📋 Собеседование",
            Description = "Объявление: 'Требуется грузчик. Оплата 4000₽/мес'.",
            Choices =
            {
                new EventChoice
                {
                    Text = "💼 Устроиться грузчиком  →  статус 🏚️ Работяга",
                    ResultMessage = "Ты получил работу!",
                    Effect = p => p.Modify(x =>
                    {
                        if (x.Job == "Бомж" || x.Job == "Безработный")
                        {
                            x.Job = "Грузчик";
                            x.Status = "🏚️ Работяга";
                            x.Energy = 100;
                        }
                    }),
                    IsAvailable = p => p.Job == "Бомж" || p.Job == "Безработный",
                    NotAvailableReason = "❌ Ты уже работаешь"
                },
                new EventChoice
                {
                    Text = "🔍 Искать получше",
                    ResultMessage = "Пока ничего.",
                    Effect = p => p.Modify(x => x.Happiness -= 5)
                }
            }
        };
    }

    public class CheapFoodStrategy : IEventStrategy
    {
        public string Name => "Еда";
        public GameEvent Generate(Player player) => new GameEvent
        {
            Title = "🍜 Обед",
            Description = "Ты голоден. Что съешь?",
            Choices =
            {
                new EventChoice
                {
                    Text = "🌯 Шаурма  →  -300₽,  +15 ❤,  +10 😊",
                    ResultMessage = "Вкусно!",
                    Effect = p => p.Modify(x => { x.Money -= 300; x.Health += 15; x.Happiness += 10; }),
                    IsAvailable = p => p.Money >= 300,
                    NotAvailableReason = "❌ Нужно 300₽"
                },
                new EventChoice
                {
                    Text = "🍜 Доширак  →  -50₽,  +5 ❤",
                    ResultMessage = "Сойдёт.",
                    Effect = p => p.Modify(x => { x.Money -= 50; x.Health += 5; }),
                    IsAvailable = p => p.Money >= 50,
                    NotAvailableReason = "❌ Нужно 50₽"
                },
                new EventChoice
                {
                    Text = "🚫 Поголодать  →  -10 ❤,  +3 🧠",
                    ResultMessage = "Закаляет характер.",
                    Effect = p => p.Modify(x => { x.Health -= 10; x.Intelligence += 3; })
                }
            }
        };
    }

    // ═══════════════════════════════════════════════
    //   ГЛАВА 3 — БИЗНЕС
    // ═══════════════════════════════════════════════

    public class BusinessIdeaStrategy : IEventStrategy
    {
        public string Name => "Бизнес-идея";
        public GameEvent Generate(Player player) => new GameEvent
        {
            Title = "💡 Бизнес-идея",
            Description = "Можешь открыть кофейню за 30000₽. Доход: +15000₽/мес К зарплате.",
            Choices =
            {
                new EventChoice
                {
                    Text = "☕ Открыть кофейню  →  -30000₽,  +15000₽/мес К ЗАРПЛАТЕ",
                    ResultMessage = "Твой бизнес открылся! Доход идёт ПОВЕРХ зарплаты.",
                    Effect = p => p.Modify(x =>
                    {
                        if (x.Money >= 30000)
                        {
                            x.Money -= 30000;
                            x.BusinessBonus += 15000;
                            x.BusinessName = "Кофейня";
                            x.Status = "💼 Бизнесмен";
                            x.Happiness += 20;
                        }
                    }),
                    IsAvailable = p => p.Money >= 30000,
                    NotAvailableReason = "❌ Нужно 30000₽"
                },
                new EventChoice
                {
                    Text = "⏳ Пока не готов",
                    ResultMessage = "Подожди.",
                    Effect = p => p.Modify(x => x.Happiness -= 3)
                }
            }
        };
    }

    // ═══════════════════════════════════════════════
    //   ГЛАВА 4 — ИМПЕРИЯ
    // ═══════════════════════════════════════════════

    public class ExpansionStrategy : IEventStrategy
    {
        public string Name => "Расширение";
        public GameEvent Generate(Player player) => new GameEvent
        {
            Title = "🏢 Расширение бизнеса",
            Description = "Вложить 100000₽ в новую точку? Доход +40000₽/мес К ЗАРПЛАТЕ.",
            Choices =
            {
                new EventChoice
                {
                    Text = "📈 Вложить  →  -100000₽,  +40000₽/мес К ЗАРПЛАТЕ",
                    ResultMessage = "Открыл вторую точку! Доход суммируется с зарплатой.",
                    Effect = p => p.Modify(x =>
                    {
                        if (x.Money >= 100000)
                        {
                            x.Money -= 100000;
                            x.BusinessBonus += 40000;
                            x.BusinessName = "Империя";
                            x.Status = "💼 Магнат";
                        }
                    }),
                    IsAvailable = p => p.Money >= 100000,
                    NotAvailableReason = "❌ Нужно 100000₽"
                },
                new EventChoice
                {
                    Text = "🛑 Не рисковать",
                    ResultMessage = "Спокойно.",
                    Effect = p => p.Modify(x => x.Happiness += 2)
                }
            }
        };
    }

    // ═══════════════════════════════════════════════
    //   ГЛАВА 5 — МИЛЛИОНЕР
    // ═══════════════════════════════════════════════

    public class CharityStrategy : IEventStrategy
    {
        public string Name => "Благотворительность";
        public GameEvent Generate(Player player) => new GameEvent
        {
            Title = "❤ Благотворительность",
            Description = "Пожертвовать 200000₽ детскому дому?",
            Choices =
            {
                new EventChoice
                {
                    Text = "💝 Пожертвовать  →  -200000₽,  +40 😊",
                    ResultMessage = "Ты стал меценатом!",
                    Effect = p => p.Modify(x =>
                    {
                        if (x.Money >= 200000)
                        {
                            x.Money -= 200000;
                            x.Happiness += 40;
                            x.Status = "🏆 Миллионер-меценат";
                        }
                    }),
                    IsAvailable = p => p.Money >= 200000,
                    NotAvailableReason = "❌ Нужно 200000₽"
                },
                new EventChoice
                {
                    Text = "❌ Отказаться",
                    ResultMessage = "Твоё право.",
                    Effect = p => p.Modify(x => x.Happiness -= 5)
                }
            }
        };
    }
}
