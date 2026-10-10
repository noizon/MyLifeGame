using MyLifeGame.Models;

namespace MyLifeGame.Patterns.EventStrategies
{
    public class SocialEventStrategy : IEventStrategy
    {
        public string Name => "Общение";

        public GameEvent Generate(Player player)
        {
            return new GameEvent
            {
                Title = "🎉 Вечеринка у друга",
                Description = "Друг зовёт на вечеринку.",
                Choices =
                {
                    new EventChoice
                    {
                        Text = "🎊 Пойти  →  +15 👥,  +15 😊,  -500₽",
                        ResultMessage = "Отлично провёл время!",
                        Effect = p => p.Modify(x =>
                        {
                            if (x.Money >= 500) { x.Money -= 500; x.Social += 15; x.Happiness += 15; }
                            else { x.Happiness -= 5; }
                        })
                    },
                    new EventChoice
                    {
                        Text = "🏠 Остаться дома  →  -5 👥",
                        ResultMessage = "Тихий вечер.",
                        Effect = p => p.Modify(x => x.Social -= 5)
                    }
                }
            };
        }
    }
}