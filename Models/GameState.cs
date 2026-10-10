using System;
using System.Collections.Generic;

namespace MyLifeGame.Models
{
    public class GameState
    {
        public Player Player { get; set; } = new Player();
        public int Turn { get; set; } = 0;
        public List<int> CompletedChapters { get; set; } = new List<int>();
        public DateTime SavedAt { get; set; } = DateTime.Now;

        public class ChapterInfo
        {
            public int Number { get; set; }
            public string Title { get; set; }
            public string GoalText { get; set; }
            public Func<Player, bool> GoalReached { get; set; }
        }

        public static List<ChapterInfo> Chapters => new List<ChapterInfo>
        {
            new ChapterInfo
            {
                Number = 1, Title = "🚮 На дне",
                GoalText = "Накопить 1000₽ и купить телефон (открывает работу)",
                GoalReached = p => p.HasAsset("phone")
            },
            new ChapterInfo
            {
                Number = 2, Title = "🏚️ Первая работа",
                GoalText = "Накопить 20000₽ и открыть банковский вклад",
                GoalReached = p => p.HasAsset("deposit")
            },
            new ChapterInfo
            {
                Number = 3, Title = "🏢 Малый бизнес",
                GoalText = "Купить кофейню ИЛИ акции ИЛИ квартиру (любой актив 3 главы)",
                GoalReached = p => p.HasAsset("coffee") || p.HasAsset("stocks") || p.HasAsset("apartment")
            },
            new ChapterInfo
            {
                Number = 4, Title = "💼 Империя",
                GoalText = "Купить магазин, ресторан или завод",
                GoalReached = p => p.HasAsset("shop") || p.HasAsset("restaurant") || p.HasAsset("factory")
            },
            new ChapterInfo
            {
                Number = 5, Title = "🏆 Миллионер",
                GoalText = "Купить небоскрёб, футбольный клуб или яхту",
                GoalReached = p => p.HasAsset("skyscraper") || p.HasAsset("football_club") || p.HasAsset("yacht")
            }
        };

        public ChapterInfo GetCurrentChapter()
        {
            int idx = Math.Clamp(Player.Chapter - 1, 0, Chapters.Count - 1);
            return Chapters[idx];
        }
    }
}