using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ajiva.Helpers
{
    public class ConsoleMenu
    {
        public async Task ShowMenu(string title, ConsoleMenuItem[] items)
        {
            Console.WriteLine(title);
            var formate = new string(Enumerable.Repeat('0', items.Length).ToArray());
            var i = 0;
            foreach (var item in items)
            {
                Console.WriteLine($"[{i.ToString(formate)}]: {item.Name}");
                i++;
            }
            int num;
            do
            {
                Console.WriteLine("Chose Action [0...{i}]");
            } while (int.TryParse(Console.ReadLine(), out num) && num < 0 && num > i);

            items[num].Action();
        }
    }

    public record ConsoleMenuItem (string Name, Action Action);
}
