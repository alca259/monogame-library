using Alca.MonoGame.Demo.Scenes;

namespace Alca.MonoGame.Demo;

/// <summary>Entry point for the MonoGame demo application.</summary>
public sealed class DemoGame : Core
{
    public DemoGame() : base("Alca MonoGame Demo", 1280, 720, false) { }

    protected override void ConfigureServices(IServiceCollection services)
    {
        services.AddTransient<UIScene_Menu>();
        services.AddTransient<UIScene_BasicControls>();
        services.AddTransient<UIScene_InputText>();
        services.AddTransient<UIScene_TextArea>();
        services.AddTransient<UIScene_Sliders>();
        services.AddTransient<UIScene_Selection>();
        services.AddTransient<UIScene_ColorPicker>();
        services.AddTransient<UIScene_Layout>();
        services.AddTransient<UIScene_ScrollView>();
        services.AddTransient<UIScene_Tooltip>();
        services.AddTransient<UIScene_Focus>();
        services.AddTransient<UIScene_Transitions>();
        services.AddTransient<EcsDemoScene>();
        services.AddTransient<Camera2DScene>();
        services.AddTransient<Physics2DScene>();
        services.AddTransient<NavigationScene>();
        services.AddTransient<AudioBasicScene>();
        services.AddTransient<AudioSpatialScene>();
        services.AddTransient<AudioAdvancedScene>();
        services.AddTransient<InputScene>();
        services.AddTransient<AnimationScene>();
        services.AddTransient<Camera3DScene>();
        services.AddTransient<ParticlesScene>();
        services.AddTransient<SpriteMaterialScene>();
        services.AddTransient<PostProcessScene>();
        services.AddTransient<ResolutionScene>();
        services.AddTransient<TiledMapScene>();
        services.AddTransient<BitmapFontScene>();
        services.AddTransient<Physics2DJointsScene>();
        services.AddTransient<SteeringScene>();
        services.AddTransient<EntityPoolScene>();
        services.AddTransient<EventBusScene>();
        services.AddTransient<StateMachineScene>();
        services.AddTransient<TimersScene>();
        services.AddTransient<TweeningScene>();
        services.AddTransient<DebugScene>();
        services.AddTransient<PersistenceScene>();
        services.AddTransient<LocalizationScene>();
        services.AddTransient<AsyncContentScene>();
        services.AddTransient<LightingScene>();
        services.AddTransient<NetworkingScene>();
        services.AddTransient<PlatformScene>();
    }

    protected override void PostInitialize()
    {
        Core.SceneManager.RequestChange(Core.GetService<UIScene_Menu>());
    }
}
