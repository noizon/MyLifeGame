using MyLifeGame.Models;

namespace MyLifeGame.Patterns.EventStrategies
{
    public class WorkEventStrategy : IEventStrategy
    {
        public string Name => "Работа";

        public GameEvent Generate(Player player)
        {
            return new GameEvent
            {
                Title = "💼 Рабочий день",
                Description = "Начальник предлагает сверхурочные. Что делать?",
                Choices =
                {
                    new EventChoice
                    {
                        Text = "Согласиться (+500₽, -10 ❤, -5 😊)",
                        ResultMessage = "Ты заработал, но устал.",
                        Effect = p => p.Modify(x => { x.Money += 500; x.Health -= 10; x.Happiness -= 5; })
                    },
                    new EventChoice
                    {
                        Text = "Отказаться (+10 😊)",
                        ResultMessage = "Ты отдохнул.",
                        Effect = p => p.Modify(x => { x.Happiness += 10; })
                    }
                }
            };
        }
    }
}