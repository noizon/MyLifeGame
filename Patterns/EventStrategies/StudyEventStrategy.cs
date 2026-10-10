using MyLifeGame.Models;

namespace MyLifeGame.Patterns.EventStrategies
{
    public class StudyEventStrategy : IEventStrategy
    {
        public string Name => "Учёба";

        public GameEvent Generate(Player player)
        {
            return new GameEvent
            {
                Title = "📚 Курсы программирования",
                Description = "Появилась возможность пройти курсы за 2000₽.",
                Choices =
                {
                    new EventChoice
                    {
                        Text = "📖 Записаться  →  -2000₽,  +15 🧠",
                        ResultMessage = "Ты стал умнее!",
                        Effect = p => p.Modify(x =>
                        {
                            if (x.Money >= 2000) { x.Money -= 2000; x.Intelligence += 15; }
                            else x.Happiness -= 5;
                        })
                    },
                    new EventChoice
                    {
                        Text = "❌ Пропустить",
                        ResultMessage = "Может, в другой раз.",
                        Effect = p => p.Modify(x => x.Happiness -= 3)
                    }
                }
            };
        }
    }
}