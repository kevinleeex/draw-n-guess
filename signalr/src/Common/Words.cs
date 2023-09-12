namespace signalr.Common;

public class Words
{
    public static readonly List<string> Items = new List<string>()
    {
        "Apple",
        "Banana",
        "Cherry",
        "Grape",
        "Kiwi",
        "Lemon",
        "Mango",
        "Orange",
        "Peach",
        "Pear",
        "Pineapple",
        "Strawberry",
        "Watermelon",
        "Dog",
        "Cat",
        "Elephant",
        "Giraffe",
        "Lion",
        "Tiger",
        "Zebra",
        "Kangaroo",
        "Horse",
        "Dolphin",
        "Penguin",
        "Panda","Smartphone",
        "Laptop",
        "Tablet",
        "Desktop Computer",
        "Smart TV",
        "Camera",
        "Game Console",
        "Headphones",
        "Router",
        "Printer",
        "Smartwatch",
        "Bluetooth Speaker",
        "E-book Reader"
    };
    
    public static string GetRandomWord()
    {
        var random = new Random();
        return Items[random.Next(Items.Count)];
    }
}