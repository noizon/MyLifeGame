using System;
using System.Collections.Generic;
using MyLifeGame.Models;
using MyLifeGame.Patterns.EventStrategies;

namespace MyLifeGame.Patterns
{
    public class EventFactory
    {
        private readonly Dictionary<int, List<IEventStrategy>> _byChapter = new Dictionary<int, List<IEventStrategy>>();
        private readonly Dictionary<int, RandomEventStrategy> _randomByChapter = new Dictionary<int, RandomEventStrategy>();
        private readonly Random _rnd = new Random();
        private readonly Dictionary<int, IEventStrategy> _lastByChapter = new Dictionary<int, IEventStrategy>();

        public EventFactory()
        {
            // Глава 1
            Register(1, new BeggingStrategy());
            Register(1, new BottleStrategy());
            Register(1, new ShelterStrategy());

            // Глава 2
            Register(2, new JobSearchStrategy());
            Register(2, new WorkEventStrategy());
            Register(2, new CheapFoodStrategy());

            // Глава 3
            Register(3, new StudyEventStrategy());
            Register(3, new BusinessIdeaStrategy());

            // Глава 4
            Register(4, new ExpansionStrategy());

            // Глава 5 
            Register(5, new CharityStrategy());

            // Общие
            Register(0, new SocialEventStrategy());

            // Генератор случайных
            for (int ch = 1; ch <= 5; ch++)
                _randomByChapter[ch] = new RandomEventStrategy(ch);
        }

        public void Register(int chapter, IEventStrategy strategy)
        {
            if (!_byChapter.ContainsKey(chapter))
                _byChapter[chapter] = new List<IEventStrategy>();
            _byChapter[chapter].Add(strategy);
        }

        public GameEvent CreateForChapter(int chapter, Player player)
        {
            
            int randomChance = chapter >= 4 ? 70 : 40;

            if (_rnd.Next(100) < randomChance && _randomByChapter.ContainsKey(chapter))
            {
                return _randomByChapter[chapter].Generate(player);
            }

            
            int commonChance = chapter >= 4 ? 10 : 15;
            if (_rnd.Next(100) < commonChance && _byChapter.ContainsKey(0) && _byChapter[0].Count > 0)
            {
                var commonList = _byChapter[0];
                var commonPick = PickNonRepeating(0, commonList);
                return commonPick.Generate(player);
            }

            // Сюжетное
            if (!_byChapter.ContainsKey(chapter) || _byChapter[chapter].Count == 0)
            {
                
                if (_randomByChapter.ContainsKey(chapter))
                    return _randomByChapter[chapter].Generate(player);

                return new GameEvent
                {
                    Title = "⏳ Обычный день",
                    Description = "Ничего особенного.",
                    Choices = new List<EventChoice>
                    {
                        new EventChoice
                        {
                            Text = "Прожить день",
                            ResultMessage = "День прошёл.",
                            Effect = p => p.Modify(x => x.Happiness += 1)
                        }
                    }
                };
            }

            var list = _byChapter[chapter];
            var pick = PickNonRepeating(chapter, list);
            return pick.Generate(player);
        }

        private IEventStrategy PickNonRepeating(int chapter, List<IEventStrategy> list)
        {
            if (list.Count == 0) return null;
            if (list.Count == 1) return list[0];

            IEventStrategy last = _lastByChapter.ContainsKey(chapter) ? _lastByChapter[chapter] : null;

            var candidates = new List<IEventStrategy>();
            foreach (var s in list)
            {
                if (last == null || s != last) candidates.Add(s);
            }

            var chosen = candidates[_rnd.Next(candidates.Count)];
            _lastByChapter[chapter] = chosen;
            return chosen;
        }

        public GameEvent CreateRandom(Player player) => CreateForChapter(player.Chapter, player);
    }
}
