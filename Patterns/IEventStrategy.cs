using MyLifeGame.Models;

namespace MyLifeGame.Patterns
{
    public interface IEventStrategy
    {
        string Name { get; }
        GameEvent Generate(Player player);
    }
}