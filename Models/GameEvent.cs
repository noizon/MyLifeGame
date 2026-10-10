using System;
using System.Collections.Generic;

namespace MyLifeGame.Models
{
    public class GameEvent
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public List<EventChoice> Choices { get; set; } = new List<EventChoice>();
    }

    public class EventChoice
    {
        public string Text { get; set; }
        public Action<Player> Effect { get; set; }
        public string ResultMessage { get; set; }

        // ⚠️ НОВОЕ: функция доступности (если null — всегда доступно)
        public Func<Player, bool> IsAvailable { get; set; }

        // ⚠️ НОВОЕ: причина недоступности (показывается если не доступно)
        public string NotAvailableReason { get; set; } = "Недоступно";

        public bool CanChoose(Player player)
        {
            return IsAvailable == null || IsAvailable(player);
        }
    }
}