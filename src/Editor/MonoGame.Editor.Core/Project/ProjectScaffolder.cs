namespace MonoGame.Editor.Core.Project;

/// <summary>
/// Genera la estructura de carpetas <c>src/</c> del proyecto de juego para un proyecto del editor recién creado.
/// Produce <c>GameApp</c> (ejecutable) y <c>GameScripts</c> (biblioteca de clases) con una solución compartida.
/// </summary>
public static class ProjectScaffolder
{
    private const string GameAppFolder     = "GameApp";
    private const string GameScriptsFolder = "GameScripts";
    private const string SrcFolder         = "src";

    /// <summary>
    /// Crea la estructura completa de subcarpetas <c>src/</c>.
    /// No hace nada si el archivo de solución ya existe (idempotente).
    /// </summary>
    public static void Scaffold(EditorProject project)
    {
        ArgumentNullException.ThrowIfNull(project);

        string srcPath         = Path.Combine(project.RootPath, SrcFolder);
        string gameAppPath     = Path.Combine(srcPath, GameAppFolder);
        string gameScriptsPath = Path.Combine(srcPath, GameScriptsFolder);

        Directory.CreateDirectory(gameAppPath);
        Directory.CreateDirectory(Path.Combine(gameAppPath, "Content"));
        Directory.CreateDirectory(Path.Combine(gameAppPath, "i18n"));
        Directory.CreateDirectory(gameScriptsPath);

        WriteIfAbsent(Path.Combine(gameAppPath, "Content", "Content.mgcb"), BuildEmptyMgcb());

        string ns = string.IsNullOrWhiteSpace(project.BaseNamespace) ? project.Name : project.BaseNamespace;

        WriteIfAbsent(Path.Combine(gameAppPath, $"{GameAppFolder}.csproj"), BuildGameAppCsproj(ns));
        WriteIfAbsent(Path.Combine(gameAppPath, "Program.cs"),              BuildProgramCs(ns));
        WriteIfAbsent(Path.Combine(gameAppPath, "Game1.cs"),                BuildGame1Cs(ns));
        WriteIfAbsent(Path.Combine(gameScriptsPath, $"{GameScriptsFolder}.csproj"), BuildGameScriptsCsproj(ns));

        string slnPath = Path.Combine(srcPath, $"{project.Name}.slnx");
        WriteIfAbsent(slnPath, BuildSlnx());
    }

    /// <summary>Devuelve la ruta absoluta al archivo de solución generado para un proyecto.</summary>
    public static string GetSolutionPath(EditorProject project) =>
        Path.Combine(project.RootPath, SrcFolder, $"{project.Name}.slnx");

    /// <summary>Devuelve la ruta absoluta al archivo .csproj de GameApp.</summary>
    public static string GetGameAppCsprojPath(EditorProject project) =>
        Path.Combine(project.RootPath, SrcFolder, GameAppFolder, $"{GameAppFolder}.csproj");

    private static void WriteIfAbsent(string path, string content)
    {
        if (!File.Exists(path))
            File.WriteAllText(path, content);
    }

    private static string BuildEmptyMgcb() =>
        "#----------------------------- Global Properties ----------------------------#\n\n" +
        "/outputDir:bin/$(Platform)\n" +
        "/intermediateDir:obj/$(Platform)\n" +
        "/platform:DesktopGL\n" +
        "/config:\n" +
        "/profile:Reach\n" +
        "/compress:False\n\n" +
        "#-------------------------------- References --------------------------------#\n\n\n" +
        "#---------------------------------- Content ---------------------------------#\n";

    private static string BuildSlnx() =>
        "<Solution>\n" +
        "  <Project Path=\"GameApp/GameApp.csproj\" />\n" +
        "  <Project Path=\"GameScripts/GameScripts.csproj\" />\n" +
        "</Solution>\n";

    private static string BuildGameAppCsproj(string ns) =>
        "<Project Sdk=\"Microsoft.NET.Sdk\">\n" +
        "  <PropertyGroup>\n" +
        "    <OutputType>Exe</OutputType>\n" +
        "    <TargetFramework>net10.0</TargetFramework>\n" +
        "    <Nullable>enable</Nullable>\n" +
        "    <ImplicitUsings>enable</ImplicitUsings>\n" +
       $"    <RootNamespace>{ns}</RootNamespace>\n" +
        "    <AssemblyName>GameApp</AssemblyName>\n" +
        "  </PropertyGroup>\n" +
        "\n" +
        "  <ItemGroup>\n" +
        "    <PackageReference Include=\"MonoGame.Framework.DesktopGL\" Version=\"3.8.*\" />\n" +
        "    <PackageReference Include=\"MonoGame.Content.Builder.Task\" Version=\"3.8.*\" />\n" +
        "    <PackageReference Include=\"Alca.MonoGame.Kernel\" Version=\"*\" />\n" +
        "  </ItemGroup>\n" +
        "\n" +
        "  <ItemGroup>\n" +
        "    <None Update=\"Content\\Content.mgcb\">\n" +
        "      <MonoGamePlatform>DesktopGL</MonoGamePlatform>\n" +
        "    </None>\n" +
        "  </ItemGroup>\n" +
        "\n" +
        "  <!-- Generador de código fuente de escenas -->\n" +
        "  <ItemGroup>\n" +
        "    <AdditionalFiles Include=\"../../../.editor/scenes/**/*.scene.json\" />\n" +
        "  </ItemGroup>\n" +
        "</Project>\n";

    private static string BuildGameScriptsCsproj(string ns) =>
        "<Project Sdk=\"Microsoft.NET.Sdk\">\n" +
        "  <PropertyGroup>\n" +
        "    <OutputType>Library</OutputType>\n" +
        "    <TargetFramework>net10.0</TargetFramework>\n" +
        "    <Nullable>enable</Nullable>\n" +
        "    <ImplicitUsings>enable</ImplicitUsings>\n" +
       $"    <RootNamespace>{ns}.Scripts</RootNamespace>\n" +
        "    <AssemblyName>GameScripts</AssemblyName>\n" +
        "  </PropertyGroup>\n" +
        "\n" +
        "  <ItemGroup>\n" +
        "    <ProjectReference Include=\"../GameApp/GameApp.csproj\" />\n" +
        "  </ItemGroup>\n" +
        "</Project>\n";

    private static string BuildProgramCs(string ns) =>
       $"using {ns};\n" +
        "\n" +
        "string scenePath = args.SkipWhile(a => a != \"--scene\").Skip(1).FirstOrDefault() ?? string.Empty;\n" +
        "using var game = new Game1(scenePath);\n" +
        "game.Run();\n";

    private static string BuildGame1Cs(string ns) =>
        "using Microsoft.Xna.Framework;\n" +
        "using Microsoft.Xna.Framework.Graphics;\n" +
        "\n" +
       $"namespace {ns};\n" +
        "\n" +
        "public sealed class Game1 : Game\n" +
        "{\n" +
        "    private readonly string _startScene;\n" +
        "    private GraphicsDeviceManager _graphics = null!;\n" +
        "    private SpriteBatch? _spriteBatch;\n" +
        "\n" +
        "    public Game1(string startScene = \"\")\n" +
        "    {\n" +
        "        _startScene = startScene;\n" +
        "        _graphics   = new GraphicsDeviceManager(this);\n" +
        "        Content.RootDirectory = \"Content\";\n" +
        "        IsMouseVisible = true;\n" +
        "    }\n" +
        "\n" +
        "    protected override void Initialize()\n" +
        "    {\n" +
        "        base.Initialize();\n" +
        "    }\n" +
        "\n" +
        "    protected override void LoadContent()\n" +
        "    {\n" +
        "        _spriteBatch = new SpriteBatch(GraphicsDevice);\n" +
        "    }\n" +
        "\n" +
        "    protected override void Update(GameTime gameTime)\n" +
        "    {\n" +
        "        base.Update(gameTime);\n" +
        "    }\n" +
        "\n" +
        "    protected override void Draw(GameTime gameTime)\n" +
        "    {\n" +
        "        GraphicsDevice.Clear(Color.CornflowerBlue);\n" +
        "        base.Draw(gameTime);\n" +
        "    }\n" +
        "}\n";
}
