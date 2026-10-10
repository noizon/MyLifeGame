using System;
using System.Collections.Generic;
using MyLifeGame.Models;
using MyLifeGame.Patterns;

namespace MyLifeGame.Services
{
    public class GameEngine
    {
        public GameState State { get; set; } = new GameState();
        private readonly EventFactory _factory = new EventFactory();
        private readonly Random _rnd = new Random();

        public event Action<GameState.ChapterInfo> OnChapterCompleted;
        public event Action<GameState.ChapterInfo> OnChapterStarted;
        public event Action<int, bool> OnTaxPaid;
        public event Action<string> OnMicroEvent;
        public event Action<string> OnChainTriggered;

        public event Action<int> OnLoanPayment;
        public event Action<int> OnLoanDefault;
        public event Action<int> OnBankrupt;
        public event Action OnLoanFullyPaid;

        public GameEvent NextTurn()
        {
            State.Turn++;

            int taxPaid = 0;
            bool taxSuccess = false;
            bool taxTriggered = false;
            int loanPayment = 0;
            int loanDefault = 0;
            bool loanFullyPaid = false;
            bool monthEnded = false;

            State.Player.Modify(p =>
            {
                // ═══════════════════════════════════════
                //   ⚡ ВРЕМЯ: 1 ход = 1 неделя
                // ═══════════════════════════════════════
                p.Week++;
                if (p.Week > 4)
                {
                    // ─── КОНЕЦ МЕСЯЦА ───
                    p.Week = 1;
                    p.Month++;
                    monthEnded = true;

                    if (p.Month > 12)
                    {
                        // ─── КОНЕЦ ГОДА ───
                        p.Month = 1;
                        p.Year++;
                        p.Age++;
                    }
                }

                // ═══════════════════════════════════════
                //   💰 ЭКОНОМИКА — доход КАЖДУЮ НЕДЕЛЮ (1/4)
                // ═══════════════════════════════════════
                p.Money += p.WeeklyIncome;

                // ═══════════════════════════════════════
                //   🏦 БАНК — платёж РАЗ В МЕСЯЦ
                // ═══════════════════════════════════════
                if (monthEnded && p.HasLoan)
                {
                    if (p.Money >= p.LoanMonthlyPayment)
                    {
                        p.Money -= p.LoanMonthlyPayment;
                        p.LoanRemaining -= p.LoanMonthlyPayment;
                        p.LoanMonthsLeft--;
                        loanPayment = p.LoanMonthlyPayment;

                        if (p.LoanRemaining <= 0)
                        {
                            p.LoanRemaining = 0;
                            p.LoanMonthsLeft = 0;
                            p.LoanMonthlyPayment = 0;
                            loanFullyPaid = true;
                            p.AddAchievement("loan_paid");
                        }
                    }
                    else
                    {
                        p.Money -= p.LoanMonthlyPayment;
                        p.LoanRemaining -= p.LoanMonthlyPayment;
                        p.LoanMonthsLeft--;
                        p.LoansDefaulted++;
                        loanDefault = p.LoanMonthlyPayment;

                        p.Health -= 5;
                        p.Happiness -= 10;

                        if (p.Money < -10000)
                        {
                            p.Health -= 10;
                            p.Happiness -= 15;
                        }
                    }
                }

                // ─── Банкрот ───
                if (p.Money < -50000)
                {
                    p.Health -= 5;
                    p.Happiness -= 5;
                }

                // ═══════════════════════════════════════
                //   🧾 НАЛОГ — РАЗ В ГОД (в январе, 1-я неделя)
                // ═══════════════════════════════════════
                if (p.Month == 1 && p.Week == 1 && p.Year > 0)
                {
                    int tax = p.CalculateYearlyTax();
                    if (tax > 0)
                    {
                        taxTriggered = true;
                        if (p.Money >= tax)
                        {
                            p.Money -= tax;
                            p.LastTaxPaid = tax;
                            p.TotalTaxPaid += tax;
                            p.Happiness -= 3;
                            taxPaid = tax;
                            taxSuccess = true;
                        }
                        else
                        {
                            taxPaid = tax;
                            p.LastTaxPaid = Math.Max(0, p.Money);
                            p.TotalTaxPaid += Math.Max(0, p.Money);
                            p.Money -= tax;
                            p.Happiness -= 20;
                            p.Health -= 10;
                            p.Status = "🚨 Должник";
                            taxSuccess = false;
                        }
                    }
                }

                // ─── Здоровье ───
                if (p.Age > 50) p.Health -= 1;
                if (p.Age > 70) p.Health -= 2;
                if (p.Age < 30 && p.Health < 100) p.Health += 1;

                // ─── Интеллект / Счастье ───
                if (p.Money > 5000) p.Intelligence += 1;
                if (p.Money < 100) p.Happiness -= 2;
                if (p.Money > 10000) p.Happiness += 1;

                // ─── Активы ───
                if (p.HasAsset("sleeping_bag")) p.Health += 5;
                if (p.HasAsset("bike")) p.Health += 1;
                if (p.HasAsset("yacht")) p.Happiness += 2;

                // ─── Семья ───
                if (p.IsMarried)
                {
                    if (p.Money > 0)
                        p.SpouseHappiness = Math.Min(100, p.SpouseHappiness + 1);
                    else
                        p.SpouseHappiness = Math.Max(0, p.SpouseHappiness - 1);
                }

                GenerateMicroEvent(p);
            });

            CheckPendingChains();

            if (loanFullyPaid) OnLoanFullyPaid?.Invoke();
            else if (loanDefault > 0) OnLoanDefault?.Invoke(loanDefault);
            else if (loanPayment > 0) OnLoanPayment?.Invoke(loanPayment);

            if (State.Player.Money < -50000 && State.Player.LoansDefaulted > 0 && _rnd.Next(100) < 10)
            {
                int debt = -State.Player.Money;
                OnBankrupt?.Invoke(debt);
            }

            if (taxTriggered)
                OnTaxPaid?.Invoke(taxPaid, taxSuccess);

            CheckChapterProgress();

            return _factory.CreateForChapter(State.Player.Chapter, State.Player);
        }

        public bool TakeLoan(int amount, int months, out string error)
        {
            error = null;
            var p = State.Player;

            if (p.HasLoan) { error = "У тебя уже есть активный кредит."; return false; }
            if (amount < 1000) { error = "Минимальная сумма — 1 000₽."; return false; }
            if (amount > 5000000) { error = "Максимум 5 000 000₽."; return false; }
            if (months < 3 || months > 60) { error = "Срок от 3 до 60 месяцев."; return false; }
            if (p.LoansDefaulted >= 2) { error = "Банк отказал: у тебя 2+ просрочки."; return false; }

            double rate;
            if (amount <= 50000) rate = 1.20;
            else if (amount <= 200000) rate = 1.30;
            else if (amount <= 1000000) rate = 1.45;
            else rate = 1.60;

            int totalToPay = (int)(amount * rate);
            int monthlyPayment = totalToPay / months;

            p.Modify(x =>
            {
                x.Money += amount;
                x.LoanAmount = amount;
                x.LoanRemaining = totalToPay;
                x.LoanMonthlyPayment = monthlyPayment;
                x.LoanMonthsLeft = months;
                x.LoansTaken++;
                x.AddAchievement("first_loan");
            });

            return true;
        }

        public bool PayOffLoan()
        {
            var p = State.Player;
            if (!p.HasLoan) return false;
            if (p.Money < p.LoanRemaining) return false;

            p.Modify(x =>
            {
                x.Money -= x.LoanRemaining;
                x.LoanRemaining = 0;
                x.LoanMonthsLeft = 0;
                x.LoanMonthlyPayment = 0;
                x.Happiness += 15;
                x.AddAchievement("loan_paid");
            });

            return true;
        }

        private void GenerateMicroEvent(Player p)
        {
            int roll = _rnd.Next(100);
            if (roll < 60) return;

            string message = null;

            if (roll < 70)
            {
                int h = _rnd.Next(1, 3);
                p.Happiness += h;
                message = $"🚶 Прогулка (+{h} 😊)";
            }
            else if (roll < 78)
            {
                int h = _rnd.Next(1, 3);
                p.Health += h;
                p.Happiness += 1;
                message = $"☕ Чашка чая (+{h} ❤, +1 😊)";
            }
            else if (roll < 85)
            {
                int iq = _rnd.Next(1, 3);
                p.Intelligence += iq;
                message = $"📖 Чтение (+{iq} 🧠)";
            }
            else if (roll < 92)
            {
                int s = _rnd.Next(1, 3);
                p.Social += s;
                message = $"👥 Встретил знакомого (+{s} 👥)";
            }
            else if (roll < 98)
            {
                int m = _rnd.Next(50, 200);
                p.Money += m;
                message = $"💵 Подработка (+{m}₽)";
            }
            else
            {
                int h = _rnd.Next(1, 3);
                p.Happiness -= h;
                message = $"🌧 Плохое настроение (-{h} 😊)";
            }

            if (message != null)
                OnMicroEvent?.Invoke(message);
        }

        public void QueueChain(string id, string description, int delayMonths)
        {
            // ⚠️ delayMonths теперь в ХОДАХ (1 ход = 1 неделя)
            // 1 месяц = 4 хода
            State.Player.PendingChains.Add(new PendingChain
            {
                Id = id,
                TriggerAtTurn = State.Turn + delayMonths * 4,
                Description = description
            });
        }

        private void CheckPendingChains()
        {
            var triggered = new List<PendingChain>();
            foreach (var chain in State.Player.PendingChains)
            {
                if (State.Turn >= chain.TriggerAtTurn)
                    triggered.Add(chain);
            }

            foreach (var chain in triggered)
            {
                State.Player.PendingChains.Remove(chain);
                ApplyChainEffect(chain.Id);
                OnChainTriggered?.Invoke(chain.Description);
            }
        }

        private void ApplyChainEffect(string chainId)
        {
            var p = State.Player;
            switch (chainId)
            {
                case "invest_win":
                    p.Modify(x => x.Money += 15000);
                    break;
                case "invest_lose":
                    p.Modify(x => { x.Happiness -= 15; x.Money -= 5000; });
                    break;
                case "friend_promise":
                    p.Modify(x => { x.Social += 15; x.Happiness += 10; });
                    break;
                case "cheat_payback":
                    p.Modify(x => { x.Health -= 20; x.Happiness -= 20; });
                    break;
            }
        }

        public bool BuyAsset(Asset asset)
        {
            if (asset == null) return false;
            var p = State.Player;

            if (p.Money < asset.Price) return false;
            if (p.HasAsset(asset.Id)) return false;
            if (p.Chapter < asset.MinChapter) return false;

            p.Modify(x =>
            {
                x.Money -= asset.Price;
                x.OwnedAssets.Add(asset.Id);
                x.Happiness += 10;

                if (asset.Id == "coffee" && x.Job == "Грузчик")
                {
                    x.Job = "Владелец кофейни";
                    x.Status = "💼 Бизнесмен";
                }
                if (asset.Id == "factory")
                {
                    x.Job = "Магнат";
                    x.Status = "🏭 Магнат";
                }
            });

            CheckChapterProgress();
            return true;
        }

        private void CheckChapterProgress()
        {
            var chapter = State.GetCurrentChapter();
            if (chapter == null) return;

            if (chapter.GoalReached(State.Player) && !State.CompletedChapters.Contains(chapter.Number))
            {
                State.CompletedChapters.Add(chapter.Number);
                OnChapterCompleted?.Invoke(chapter);

                if (State.Player.Chapter < GameState.Chapters.Count)
                {
                    State.Player.Chapter++;
                    State.Player.Modify(p => p.Happiness += 20);
                    OnChapterStarted?.Invoke(State.GetCurrentChapter());
                }
            }
        }

        public void ApplyChoice(EventChoice choice)
        {
            choice.Effect?.Invoke(State.Player);

            if (choice.Text.Contains("Вложить"))
            {
                if (_rnd.Next(100) < 60)
                    QueueChain("invest_win", "Через 3 месяца ты получишь доход от инвестиции!", 3);
                else
                    QueueChain("invest_lose", "Через 3 месяца выяснится, что инвестиция провалилась.", 3);
            }

            CheckChapterProgress();
        }

        public void Reset()
        {
            State = new GameState();
        }
    }
}