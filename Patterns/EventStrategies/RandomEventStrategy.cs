using System;
using System.Collections.Generic;
using MyLifeGame.Models;

namespace MyLifeGame.Patterns.EventStrategies
{
    public class RandomEventStrategy : IEventStrategy
    {
        public string Name => "Случайное событие";

        private readonly Random _rnd = new Random();
        private readonly int _chapter;

        public RandomEventStrategy(int chapter)
        {
            _chapter = chapter;
        }

        public GameEvent Generate(Player player)
        {
            // Приоритет: семья, универ, путешествия — если условия позволяют
            int roll = _rnd.Next(100);

            if (roll < 25) return GenerateSeasonalEvent(player);
            if (roll < 40) return GenerateRandomEncounter(player);

            // Специальные события
            int specialRoll = _rnd.Next(100);
            if (specialRoll < 15 && !player.IsMarried && player.Age >= 20 && player.Money >= 5000)
                return GenerateWeddingEvent(player);
            if (specialRoll < 25 && player.IsMarried && player.ChildrenCount < 5 && player.Money >= 10000)
                return GenerateChildEvent(player);
            if (specialRoll < 40 && !player.HasEducation && player.Age >= 17 && player.Age <= 30 && player.Money >= 15000)
                return GenerateUniversityEvent(player);
            if (specialRoll < 50 && player.HasEducation && player.EducationLevel < 3)
                return GenerateEducationUpgradeEvent(player);
            if (specialRoll < 65 && player.Money >= 20000)
                return GenerateTravelEvent(player);

            // Обычные категории
            roll = _rnd.Next(100);
            if (roll < 30) return GenerateWorkEvent(player);
            if (roll < 55) return GenerateMoneyEvent(player);
            if (roll < 75) return GenerateSocialEvent(player);
            if (roll < 90) return GenerateHealthEvent(player);
            return GenerateStudyEvent(player);
        }

        // ═══════════════════════════════════════════════
        //   💍 СВАДЬБА (только женские имена)
        // ═══════════════════════════════════════════════
        private GameEvent GenerateWeddingEvent(Player player)
        {
            string[] femaleNames =
            {
                "Марина", "Анна", "Ольга", "Екатерина", "Елена",
                "Наташа", "Ирина", "Света", "Юля", "Алина",
                "Кристина", "Виктория", "Дарья", "Полина", "Ксения",
                "Вероника", "Алиса", "София", "Милана", "Валерия",
                "Татьяна", "Лариса", "Оксана", "Инна", "Мария"
            };

            string partner = femaleNames[_rnd.Next(femaleNames.Length)];
            int weddingCost = 5000 + _chapter * 3000;

            string[] phrases =
            {
                $"Ты встретил {partner} в кафе. Она тебе понравилась.",
                $"Ваши взгляды встретились в парке. {partner} улыбнулась.",
                $"Друзья познакомили тебя с {partner}. Она — та самая.",
                $"Ты случайно столкнулся с {partner} на улице. Судьба?",
                $"На работе появилась новая коллега — {partner}. Ты влюбился.",
                $"Вы вместе стояли в очереди. {partner} заговорила первой.",
                $"Ты увидел {partner} на вечеринке. Сердце ёкнуло.",
                $"В библиотеке {partner} попросила помочь с книгой."
            };
            string description = phrases[_rnd.Next(phrases.Length)];

            return new GameEvent
            {
                Title = $"💍 Свадьба с {partner}",
                Description = description + " Готов ли ты к свадьбе?",
                Choices =
                {
                    new EventChoice
                    {
                        Text = $"💐 Сыграть свадьбу → -{weddingCost:N0}₽, +30 😊, +15 👥",
                        ResultMessage = $"Ты женился на {partner}! Теперь вы семья.",
                        Effect = p => p.Modify(x =>
                        {
                            x.Money -= weddingCost;
                            x.SpouseName = partner;
                            x.SpouseHappiness = 70;
                            x.Happiness += 30;
                            x.Social += 15;
                            x.AddAchievement("married");
                        }),
                        IsAvailable = p => p.Money >= weddingCost,
                        NotAvailableReason = $"❌ Нужно {weddingCost:N0}₽"
                    },
                    new EventChoice
                    {
                        Text = "🚶 Пока не готов",
                        ResultMessage = "Возможно, в другой раз.",
                        Effect = p => p.Modify(x => x.Happiness -= 5)
                    }
                }
            };
        }

        // ═══════════════════════════════════════════════
        //   👶 РЕБЁНОК
        // ═══════════════════════════════════════════════
        private GameEvent GenerateChildEvent(Player player)
        {
            string[] boyNames = { "Иван", "Максим", "Артём", "Никита", "Егор",
                                  "Миша", "Саша", "Дима", "Кирилл", "Лёша" };
            string[] girlNames = { "Аня", "Маша", "Катя", "Лиза", "Соня",
                                   "Даша", "Полина", "Вера", "Настя", "Ксюша" };

            bool isBoy = _rnd.Next(2) == 0;
            string babyName = isBoy
                ? boyNames[_rnd.Next(boyNames.Length)]
                : girlNames[_rnd.Next(girlNames.Length)];

            int childCost = 10000;

            string[] phrases =
            {
                $"{player.SpouseName} сообщила радостную новость — она беременна!",
                $"{player.SpouseName} с улыбкой показала тебе тест. Две полоски!",
                $"Врач подтвердил — {player.SpouseName} ждёт ребёнка!"
            };
            string description = phrases[_rnd.Next(phrases.Length)];

            return new GameEvent
            {
                Title = "👶 Пополнение в семье",
                Description = description + " Ты готов стать отцом?",
                Choices =
                {
                    new EventChoice
                    {
                        Text = $"❤ Родить ребёнка → -{childCost:N0}₽, +20 😊, +10 👥",
                        ResultMessage = $"Родил{(isBoy ? "ся сын" : "ась дочь")} {babyName}! Семья выросла.",
                        Effect = p => p.Modify(x =>
                        {
                            x.Money -= childCost;
                            x.ChildrenCount++;
                            x.ChildrenNames.Add(babyName);
                            x.SpouseHappiness = Math.Min(100, x.SpouseHappiness + 15);
                            x.Happiness += 20;
                            x.Social += 10;

                            if (x.ChildrenCount >= 3)
                                x.AddAchievement("big_family");
                        }),
                        IsAvailable = p => p.Money >= childCost,
                        NotAvailableReason = $"❌ Нужно {childCost:N0}₽"
                    },
                    new EventChoice
                    {
                        Text = "🚫 Пока не время",
                        ResultMessage = $"{player.SpouseName} расстроилась.",
                        Effect = p => p.Modify(x =>
                        {
                            x.SpouseHappiness = Math.Max(0, x.SpouseHappiness - 15);
                            x.Happiness -= 5;
                        })
                    }
                }
            };
        }

        // ═══════════════════════════════════════════════
        //   🎓 УНИВЕРСИТЕТ
        // ═══════════════════════════════════════════════
        private GameEvent GenerateUniversityEvent(Player player)
        {
            int tuition = 15000 + _chapter * 5000;

            return new GameEvent
            {
                Title = "🎓 Поступление в университет",
                Description = $"Ты можешь поступить в вуз за {tuition:N0}₽. Специальность даст +15% к зарплате.",
                Choices =
                {
                    new EventChoice
                    {
                        Text = $"💻 Программист → -{tuition:N0}₽, +15% доход",
                        ResultMessage = "Ты поступил на программиста!",
                        Effect = p => p.Modify(x =>
                        {
                            x.Money -= tuition;
                            x.University = "МГУ";
                            x.Education = "Программист";
                            x.EducationLevel = 1;
                            x.Intelligence += 10;
                            x.AddAchievement("student");
                        }),
                        IsAvailable = p => p.Money >= tuition,
                        NotAvailableReason = $"❌ Нужно {tuition:N0}₽"
                    },
                    new EventChoice
                    {
                        Text = $"⚕ Врач → -{tuition:N0}₽, +15% доход",
                        ResultMessage = "Ты поступил на врача!",
                        Effect = p => p.Modify(x =>
                        {
                            x.Money -= tuition;
                            x.University = "Сеченовский";
                            x.Education = "Врач";
                            x.EducationLevel = 1;
                            x.Intelligence += 12;
                            x.AddAchievement("student");
                        }),
                        IsAvailable = p => p.Money >= tuition,
                        NotAvailableReason = $"❌ Нужно {tuition:N0}₽"
                    },
                    new EventChoice
                    {
                        Text = $"⚖ Юрист → -{tuition:N0}₽, +15% доход",
                        ResultMessage = "Ты поступил на юриста!",
                        Effect = p => p.Modify(x =>
                        {
                            x.Money -= tuition;
                            x.University = "МГЮА";
                            x.Education = "Юрист";
                            x.EducationLevel = 1;
                            x.Intelligence += 8;
                            x.AddAchievement("student");
                        }),
                        IsAvailable = p => p.Money >= tuition,
                        NotAvailableReason = $"❌ Нужно {tuition:N0}₽"
                    },
                    new EventChoice
                    {
                        Text = "🚫 Не учиться",
                        ResultMessage = "Универ — не для всех.",
                        Effect = p => p.Modify(x => x.Happiness += 2)
                    }
                }
            };
        }

        // ═══════════════════════════════════════════════
        //   📚 ПОВЫШЕНИЕ ОБРАЗОВАНИЯ
        // ═══════════════════════════════════════════════
        private GameEvent GenerateEducationUpgradeEvent(Player player)
        {
            int upgradeCost = 30000 + player.EducationLevel * 20000;
            string nextLevel = player.EducationLevel == 1 ? "магистратура" : "PhD";

            return new GameEvent
            {
                Title = $"📚 Повышение квалификации ({nextLevel})",
                Description = $"Ты можешь пойти на {nextLevel} за {upgradeCost:N0}₽. Бонус к доходу вырастет.",
                Choices =
                {
                    new EventChoice
                    {
                        Text = $"🎓 Поступить → -{upgradeCost:N0}₽, +15% доход",
                        ResultMessage = $"Ты поступил на {nextLevel}!",
                        Effect = p => p.Modify(x =>
                        {
                            x.Money -= upgradeCost;
                            x.EducationLevel++;
                            x.Intelligence += 15;
                            x.AddAchievement("educated");
                        }),
                        IsAvailable = p => p.Money >= upgradeCost,
                        NotAvailableReason = $"❌ Нужно {upgradeCost:N0}₽"
                    },
                    new EventChoice
                    {
                        Text = "🚫 Достаточно",
                        ResultMessage = "Уже неплохо.",
                        Effect = p => p.Modify(x => x.Happiness += 2)
                    }
                }
            };
        }

        // ═══════════════════════════════════════════════
        //   ✈️ ПУТЕШЕСТВИЯ
        // ═══════════════════════════════════════════════
        private GameEvent GenerateTravelEvent(Player player)
        {
            var trips = new (string country, string emoji, int cost, int iq, int happy)[]
            {
                ("Египет", "🇪🇬", 20000, 5, 20),
                ("Турция", "🇹🇷", 25000, 5, 20),
                ("Таиланд", "🇹🇭", 30000, 7, 22),
                ("Япония", "🇯🇵", 50000, 10, 25),
                ("Италия", "🇮🇹", 45000, 10, 25),
                ("Франция", "🇫🇷", 55000, 12, 25),
                ("США", "🇺🇸", 80000, 15, 30),
                ("Австралия", "🇦🇺", 100000, 15, 30),
                ("Мальдивы", "🏝️", 70000, 8, 35),
            };

            var trip = trips[_rnd.Next(trips.Length)];

            return new GameEvent
            {
                Title = $"✈️ Поездка в {trip.country}",
                Description = $"Можно съездить в {trip.country} за {trip.cost:N0}₽. Даст +{trip.iq} 🧠 и +{trip.happy} 😊.",
                Choices =
                {
                    new EventChoice
                    {
                        Text = $"🌍 Полетели! → -{trip.cost:N0}₽, +{trip.iq} 🧠, +{trip.happy} 😊",
                        ResultMessage = $"Ты посетил {trip.country}! Незабываемые впечатления.",
                        Effect = p => p.Modify(x =>
                        {
                            x.Money -= trip.cost;
                            x.Intelligence += trip.iq;
                            x.Happiness += trip.happy;
                            x.TripsCount++;
                            x.VisitedCountries.Add(trip.country);

                            if (x.TripsCount >= 5)
                                x.AddAchievement("traveler");
                        }),
                        IsAvailable = p => p.Money >= trip.cost,
                        NotAvailableReason = $"❌ Нужно {trip.cost:N0}₽"
                    },
                    new EventChoice
                    {
                        Text = "🏠 Остаться дома",
                        ResultMessage = "В другой раз.",
                        Effect = p => p.Modify(x => x.Happiness += 2)
                    }
                }
            };
        }

        // ═══════════════════════════════════════════════
        //   СЕЗОННЫЕ СОБЫТИЯ
        // ═══════════════════════════════════════════════
        private GameEvent GenerateSeasonalEvent(Player player)
        {
            int season = player.Month;

            if (season == 12 || season == 1 || season == 2)
            {
                return new GameEvent
                {
                    Title = "🎄 Зимний вечер",
                    Description = "Мороз, снег, уют.",
                    Choices =
                    {
                        new EventChoice
                        {
                            Text = "🔥 Посидеть у камина → +10 😊",
                            ResultMessage = "Тепло и спокойно.",
                            Effect = p => p.Modify(x => x.Happiness += 10)
                        },
                        new EventChoice
                        {
                            Text = "⛸ Пойти на каток → -500₽, +15 😊, +5 ❤",
                            ResultMessage = "Весело провёл время!",
                            Effect = p => p.Modify(x => { x.Money -= 500; x.Happiness += 15; x.Health += 5; }),
                            IsAvailable = p => p.Money >= 500,
                            NotAvailableReason = "❌ Нужно 500₽"
                        }
                    }
                };
            }

            if (season >= 3 && season <= 5)
            {
                return new GameEvent
                {
                    Title = "🌸 Весеннее настроение",
                    Description = "Природа оживает.",
                    Choices =
                    {
                        new EventChoice
                        {
                            Text = "🌳 Прогулка в парке → +10 😊, +5 ❤",
                            ResultMessage = "Свежий воздух!",
                            Effect = p => p.Modify(x => { x.Happiness += 10; x.Health += 5; })
                        }
                    }
                };
            }

            if (season >= 6 && season <= 8)
            {
                return new GameEvent
                {
                    Title = "☀ Летняя жара",
                    Description = "+35°C на улице.",
                    Choices =
                    {
                        new EventChoice
                        {
                            Text = "🏖 Пойти на пляж → +15 😊",
                            ResultMessage = "Загораешь!",
                            Effect = p => p.Modify(x => x.Happiness += 15)
                        },
                        new EventChoice
                        {
                            Text = "💼 Работать → +2000₽, -10 😊",
                            ResultMessage = "Деньги не лишние.",
                            Effect = p => p.Modify(x => { x.Money += 2000; x.Happiness -= 10; })
                        }
                    }
                };
            }

            return new GameEvent
            {
                Title = "🍂 Осенняя хандра",
                Description = "Дожди и тоска.",
                Choices =
                {
                    new EventChoice
                    {
                        Text = "☕ Тёплый чай дома → +5 😊",
                        ResultMessage = "Уютно.",
                        Effect = p => p.Modify(x => x.Happiness += 5)
                    },
                    new EventChoice
                    {
                        Text = "🎬 Пойти в кино → -400₽, +10 😊",
                        ResultMessage = "Отвлёкся.",
                        Effect = p => p.Modify(x => { x.Money -= 400; x.Happiness += 10; }),
                        IsAvailable = p => p.Money >= 400,
                        NotAvailableReason = "❌ Нужно 400₽"
                    }
                }
            };
        }

        // ═══════════════════════════════════════════════
        //   СЛУЧАЙНЫЕ ВСТРЕЧИ
        // ═══════════════════════════════════════════════
        private GameEvent GenerateRandomEncounter(Player player)
        {
            int roll = _rnd.Next(100);

            if (roll < 25)
            {
                return new GameEvent
                {
                    Title = "🚨 Авария на улице",
                    Description = "Ты видишь ДТП. Водитель без сознания.",
                    Choices =
                    {
                        new EventChoice
                        {
                            Text = "🆘 Вызвать скорую → +15 😊",
                            ResultMessage = "Ты спас человека!",
                            Effect = p => p.Modify(x => x.Happiness += 15)
                        },
                        new EventChoice
                        {
                            Text = "🚶 Пройти мимо → -10 😊",
                            ResultMessage = "Совесть мучает.",
                            Effect = p => p.Modify(x => x.Happiness -= 10)
                        }
                    }
                };
            }

            if (roll < 50)
            {
                return new GameEvent
                {
                    Title = "🎸 Уличный музыкант",
                    Description = "Классно играет.",
                    Choices =
                    {
                        new EventChoice
                        {
                            Text = "💰 Дать 100₽ → -100₽, +5 😊",
                            ResultMessage = "Хорошо поёт!",
                            Effect = p => p.Modify(x => { x.Money -= 100; x.Happiness += 5; }),
                            IsAvailable = p => p.Money >= 100,
                            NotAvailableReason = "❌ Нужно 100₽"
                        },
                        new EventChoice
                        {
                            Text = "🎧 Послушать → +5 😊",
                            ResultMessage = "Приятная музыка.",
                            Effect = p => p.Modify(x => x.Happiness += 5)
                        }
                    }
                };
            }

            if (roll < 75)
            {
                string[] names = { "Алексей", "Марина", "Дмитрий", "Ольга", "Сергей", "Анна" };
                string friend = names[_rnd.Next(names.Length)];

                return new GameEvent
                {
                    Title = $"👋 Встретил старого друга ({friend})",
                    Description = "Не виделись годами!",
                    Choices =
                    {
                        new EventChoice
                        {
                            Text = "☕ Кафе → -500₽, +15 😊, +10 👥",
                            ResultMessage = "Отлично посидели!",
                            Effect = p => p.Modify(x => { x.Money -= 500; x.Happiness += 15; x.Social += 10; }),
                            IsAvailable = p => p.Money >= 500,
                            NotAvailableReason = "❌ Нужно 500₽"
                        },
                        new EventChoice
                        {
                            Text = "🤝 Обняться → +5 😊",
                            ResultMessage = "Приятная встреча.",
                            Effect = p => p.Modify(x => x.Happiness += 5)
                        }
                    }
                };
            }

            return new GameEvent
            {
                Title = "🐕 Бродячая собака",
                Description = "Худая, голодная.",
                Choices =
                {
                    new EventChoice
                    {
                        Text = "🍖 Покормить → -300₽, +15 😊",
                        ResultMessage = "Собака виляет хвостом!",
                        Effect = p => p.Modify(x => { x.Money -= 300; x.Happiness += 15; }),
                        IsAvailable = p => p.Money >= 300,
                        NotAvailableReason = "❌ Нужно 300₽"
                    },
                    new EventChoice
                    {
                        Text = "🚶 Уйти",
                        ResultMessage = "Она справится.",
                        Effect = p => p.Modify(x => x.Happiness -= 3)
                    }
                }
            };
        }

        // ═══════════════════════════════════════════════
        //   РАБОТА
        // ═══════════════════════════════════════════════
        private GameEvent GenerateWorkEvent(Player player)
        {
            string[] jobs = { "грузчик", "курьер", "продавец", "официант", "уборщик",
                "сторож", "водитель", "строитель", "упаковщик", "мойщик" };

            string job = jobs[_rnd.Next(jobs.Length)];
            int baseSalary = 3000 + _chapter * 2000;
            int salary = baseSalary + _rnd.Next(-500, 1500);
            int healthCost = _rnd.Next(5, 20);
            int energyCost = _rnd.Next(5, 15);

            return new GameEvent
            {
                Title = $"💼 Подработка: {job}",
                Description = $"Тебе предложили подработать {job}ом за {salary:N0}₽/мес.",
                Choices =
                {
                    new EventChoice
                    {
                        Text = $"💪 Согласиться → +{salary:N0}₽/мес, -{healthCost} ❤",
                        ResultMessage = "Ты устроился!",
                        Effect = p => p.Modify(x =>
                        {
                            x.Job = char.ToUpper(job[0]) + job.Substring(1);
                            x.Money += salary / 3;
                            x.Health -= healthCost;
                            x.Energy -= energyCost;
                        })
                    },
                    new EventChoice
                    {
                        Text = "🚫 Отказаться",
                        ResultMessage = "Может, найдёшь получше.",
                        Effect = p => p.Modify(x => x.Happiness -= 3)
                    }
                }
            };
        }

        // ═══════════════════════════════════════════════
        //   ДЕНЬГИ
        // ═══════════════════════════════════════════════
        private GameEvent GenerateMoneyEvent(Player player)
        {
            int roll = _rnd.Next(100);

            if (roll < 30)
            {
                int amount = _rnd.Next(100, 500 + _chapter * 200);
                return new GameEvent
                {
                    Title = "💰 Находка",
                    Description = $"Ты нашёл кошелёк с {amount:N0}₽.",
                    Choices =
                    {
                        new EventChoice
                        {
                            Text = $"😈 Забрать → +{amount:N0}₽, -5 😊",
                            ResultMessage = "Совесть мучает.",
                            Effect = p => p.Modify(x => { x.Money += amount; x.Happiness -= 5; })
                        },
                        new EventChoice
                        {
                            Text = "😇 В полицию → +10 😊",
                            ResultMessage = "Ты честный.",
                            Effect = p => p.Modify(x => x.Happiness += 10)
                        }
                    }
                };
            }

            if (roll < 60)
            {
                int ticket = 500 + _chapter * 200;
                int win = ticket * _rnd.Next(5, 30);
                return new GameEvent
                {
                    Title = "🎰 Лотерея",
                    Description = $"Билет {ticket:N0}₽. Выигрыш: {win:N0}₽.",
                    Choices =
                    {
                        new EventChoice
                        {
                            Text = $"🎟 Купить → -{ticket:N0}₽",
                            ResultMessage = "Проверяем...",
                            Effect = p => p.Modify(x =>
                            {
                                x.Money -= ticket;
                                if (_rnd.Next(100) < 25) { x.Money += win; x.Happiness += 20; }
                            }),
                            IsAvailable = p => p.Money >= ticket,
                            NotAvailableReason = $"❌ Нужно {ticket:N0}₽"
                        },
                        new EventChoice
                        {
                            Text = "🚫 Не играть",
                            ResultMessage = "Лотерея — налог на глупость.",
                            Effect = p => p.Modify(x => x.Happiness += 2)
                        }
                    }
                };
            }

            int invest = 2000 + _chapter * 1000;
            return new GameEvent
            {
                Title = "📈 Инвестиция",
                Description = $"Друг предлагает вложить {invest:N0}₽. 60% удвоить.",
                Choices =
                {
                    new EventChoice
                    {
                        Text = $"💸 Вложить {invest:N0}₽",
                        ResultMessage = "Считаем...",
                        Effect = p => p.Modify(x =>
                        {
                            x.Money -= invest;
                            if (_rnd.Next(100) < 60) x.Money += invest * 2;
                            else x.Happiness -= 10;
                        }),
                        IsAvailable = p => p.Money >= invest,
                        NotAvailableReason = $"❌ Нужно {invest:N0}₽"
                    },
                    new EventChoice
                    {
                        Text = "🚫 Отказаться",
                        ResultMessage = "Риск — не сегодня.",
                        Effect = p => p.Modify(x => x.Happiness += 2)
                    }
                }
            };
        }

        // ═══════════════════════════════════════════════
        //   ОБЩЕНИЕ
        // ═══════════════════════════════════════════════
        private GameEvent GenerateSocialEvent(Player player)
        {
            string[] names = { "Алексей", "Марина", "Дмитрий", "Ольга", "Сергей", "Анна", "Игорь", "Катя" };
            string[] personalities = { "добрый", "весёлый", "умный", "жадный", "щедрый" };

            int roll = _rnd.Next(100);

            if (roll < 40)
            {
                string name = names[_rnd.Next(names.Length)];
                string personality = personalities[_rnd.Next(personalities.Length)];

                return new GameEvent
                {
                    Title = $"👋 {name}",
                    Description = $"Ты познакомился с {name}.",
                    Choices =
                    {
                        new EventChoice
                        {
                            Text = "🤝 Подружиться → +10 👥, +10 😊",
                            ResultMessage = $"Ты дружишь с {name}.",
                            Effect = p => p.Modify(x =>
                            {
                                x.Social += 10;
                                x.Happiness += 10;
                                var existing = x.FindNpc(name);
                                if (existing == null)
                                {
                                    x.KnownNpcs.Add(new Npc
                                    {
                                        Name = name,
                                        Personality = personality,
                                        Friendship = 20,
                                        MetAtMonth = x.Month
                                    });
                                }
                            })
                        },
                        new EventChoice
                        {
                            Text = "🚶 Пройти мимо",
                            ResultMessage = "Не сегодня.",
                            Effect = p => p.Modify(x => x.Happiness -= 2)
                        }
                    }
                };
            }

            if (roll < 60 && player.KnownNpcs.Count > 0)
            {
                var npc = player.KnownNpcs[_rnd.Next(player.KnownNpcs.Count)];

                return new GameEvent
                {
                    Title = $"💬 {npc.Name} заходит в гости",
                    Description = $"Друг {npc.Name} зашёл поболтать.",
                    Choices =
                    {
                        new EventChoice
                        {
                            Text = "☕ Угостить чаем → +5 😊",
                            ResultMessage = "Приятный вечер.",
                            Effect = p => p.Modify(x =>
                            {
                                x.Happiness += 5;
                                var n = x.FindNpc(npc.Name);
                                if (n != null) n.Friendship = Math.Min(100, n.Friendship + 5);
                            })
                        }
                    }
                };
            }

            return new GameEvent
            {
                Title = "🚶 Одинокий вечер",
                Description = "Ты один дома.",
                Choices =
                {
                    new EventChoice
                    {
                        Text = "📺 Кино → +5 😊",
                        ResultMessage = "Спокойно.",
                        Effect = p => p.Modify(x => x.Happiness += 5)
                    }
                }
            };
        }

        // ═══════════════════════════════════════════════
        //   ЗДОРОВЬЕ
        // ═══════════════════════════════════════════════
        private GameEvent GenerateHealthEvent(Player player)
        {
            int roll = _rnd.Next(100);

            if (roll < 40)
            {
                return new GameEvent
                {
                    Title = "🤒 Простуда",
                    Description = "Ты заболел.",
                    Choices =
                    {
                        new EventChoice
                        {
                            Text = "💊 Лекарства → -500₽, +15 ❤",
                            ResultMessage = "Поправился.",
                            Effect = p => p.Modify(x => { x.Money -= 500; x.Health += 15; }),
                            IsAvailable = p => p.Money >= 500,
                            NotAvailableReason = "❌ Нужно 500₽"
                        },
                        new EventChoice
                        {
                            Text = "🚶 Перетерпеть → -15 ❤",
                            ResultMessage = "Затянулось.",
                            Effect = p => p.Modify(x => x.Health -= 15)
                        }
                    }
                };
            }

            return new GameEvent
            {
                Title = "🏃 Спортзал",
                Description = "Решил заняться спортом.",
                Choices =
                {
                    new EventChoice
                    {
                        Text = "💪 Усердно → +15 ❤, -10 😊",
                        ResultMessage = "Мышцы болят.",
                        Effect = p => p.Modify(x => { x.Health += 15; x.Happiness -= 10; })
                    },
                    new EventChoice
                    {
                        Text = "🧘 Размяться → +5 ❤",
                        ResultMessage = "Лёгкая зарядка.",
                        Effect = p => p.Modify(x => x.Health += 5)
                    }
                }
            };
        }

        // ═══════════════════════════════════════════════
        //   УЧЁБА
        // ═══════════════════════════════════════════════
        private GameEvent GenerateStudyEvent(Player player)
        {
            int coursePrice = 1000 + _chapter * 500;
            int iqGain = 10 + _chapter * 3;

            return new GameEvent
            {
                Title = "📚 Онлайн-курсы",
                Description = $"Курсы за {coursePrice:N0}₽. Дадут +{iqGain} 🧠.",
                Choices =
                {
                    new EventChoice
                    {
                        Text = $"📖 Купить → -{coursePrice:N0}₽, +{iqGain} 🧠",
                        ResultMessage = "Прокачал мозги!",
                        Effect = p => p.Modify(x => { x.Money -= coursePrice; x.Intelligence += iqGain; }),
                        IsAvailable = p => p.Money >= coursePrice,
                        NotAvailableReason = $"❌ Нужно {coursePrice:N0}₽"
                    },
                    new EventChoice
                    {
                        Text = "🚫 Пропустить",
                        ResultMessage = "Может, само придёт.",
                        Effect = p => p.Modify(x => x.Happiness -= 2)
                    }
                }
            };
        }
    }
}