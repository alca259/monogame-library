namespace Alca.MonoGame.Demo;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        using var game = new DemoGame();
        game.Run();
    }
}
