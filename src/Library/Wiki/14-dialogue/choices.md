# Sistema de Opciones — DialogueBoxBehaviour y ChoicesPanelBehaviour

**Namespace:** `Alca.MonoGame.Kernel.Dialogue`

Los behaviours de presentación son la capa visual del sistema de diálogos. Ambos son `sealed GameBehaviour` que se añaden a una entidad del mundo y dibujan la UI de diálogo automáticamente. Se suscriben a los eventos del `DialogueManager` en su ciclo `Initialize`/`Start` y no necesitan código adicional en el `Update` del juego (aunque pueden sobreescribirse si se necesita lógica personalizada).

---

## DialogueBoxBehaviour

Dibuja el fondo de la caja de diálogo, el nombre del personaje y el texto revelado por `TypewriterEffect`.

### Constructor

```csharp
DialogueBoxBehaviour(SpriteFont font, DialogueManager manager, Texture2D? boxTexture = null)
```

### Propiedades

| Propiedad | Tipo | Descripción |
|---|---|---|
| `IsVisible` | `bool` | Se pone a `true` automáticamente cuando `manager.IsActive`; `false` al terminar |
| `BoxColor` | `Color` | Color de fondo de la caja (default: semitransparente oscuro) |
| `TextColor` | `Color` | Color del texto del diálogo (default: `Color.White`) |
| `SpeakerColor` | `Color` | Color del nombre del personaje (default: `Color.Yellow`) |
| `BoxBounds` | `Rectangle` | Área de la caja en coordenadas de pantalla |
| `TypewriterEffect` | `TypewriterEffect` | Efecto de escritura interno; accesible para configurar `CharsPerSecond` |

### Configuración

```csharp
using Alca.MonoGame.Kernel.Dialogue;
using Alca.MonoGame.Kernel.ECS;

// En LoadContent de la escena:
SpriteFont dialogueFont = Content.Load<SpriteFont>("Fonts/DialogueFont");
Texture2D  boxTexture   = Content.Load<Texture2D>("UI/DialogueBox");

DialogueManager manager = World.DialogueManager!;

GameEntity dialogueEntity = World.CreateEntity("DialogueUI", Vector2.Zero);

var box = new DialogueBoxBehaviour(dialogueFont, manager, boxTexture)
{
    BoxBounds    = new Rectangle(32, 420, 960, 160),
    TextColor    = Color.White,
    SpeakerColor = new Color(255, 220, 80),
};
box.TypewriterEffect.CharsPerSecond = 35f;

dialogueEntity.AddBehaviour(box);
```

---

## ChoicesPanelBehaviour

Dibuja la lista de opciones filtradas por `EvaluateCondition`. El jugador selecciona con las teclas 1–4 (o equivalente de gamepad).

### Constructor

```csharp
ChoicesPanelBehaviour(SpriteFont font, DialogueManager manager, Texture2D? panelTexture = null)
```

### Propiedades

| Propiedad | Tipo | Descripción |
|---|---|---|
| `IsVisible` | `bool` | `true` automáticamente cuando la línea actual tiene `HasChoices = true` |
| `PanelColor` | `Color` | Color de fondo del panel de opciones |
| `ChoiceColor` | `Color` | Color del texto de las opciones |
| `SelectedChoiceColor` | `Color` | Color de la opción resaltada |
| `PanelBounds` | `Rectangle` | Área del panel en coordenadas de pantalla |

### Configuración

```csharp
var choices = new ChoicesPanelBehaviour(dialogueFont, manager, panelTexture)
{
    PanelBounds        = new Rectangle(32, 300, 480, 120),
    ChoiceColor        = Color.LightGray,
    SelectedChoiceColor = Color.White,
};
dialogueEntity.AddBehaviour(choices);
```

---

## Configuración de una escena de diálogo completa

```csharp
using Alca.MonoGame.Kernel.Dialogue;
using Alca.MonoGame.Kernel.ECS;
using Alca.MonoGame.Kernel.Scenes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public sealed class VillageScene : Scene
{
    protected override GameWorld? CreateWorld()
    {
        return new GameWorld
        {
            DialogueManager = new DialogueManager
            {
                EvaluateCondition = cond =>
                    cond.IsEmpty || GameFlags.Check(cond.Key, cond.Value)
            }
        };
    }

    protected override void LoadContent()
    {
        SpriteFont font        = Content.Load<SpriteFont>("Fonts/Main");
        Texture2D  boxTex      = Content.Load<Texture2D>("UI/DialogueBox");
        Texture2D  choicesTex  = Content.Load<Texture2D>("UI/ChoicesPanel");

        DialogueManager manager = World!.DialogueManager!;

        // Construir el script
        DialogueScript script = new DialogueScript.Builder()
            .AddLine("npc_elder", "elder.welcome", "portrait_elder")
                .WithChoice("choice.tell_me_more", nextIndex: 1)
                .WithChoice("choice.leave",         nextIndex: -1)
            .AddLine("npc_elder", "elder.legend",  "portrait_elder")
            .Build();

        // Entidad de UI de diálogo
        GameEntity uiEntity = World.CreateEntity("DialogueUI", Vector2.Zero);

        uiEntity.AddBehaviour(new DialogueBoxBehaviour(font, manager, boxTex)
        {
            BoxBounds = new Rectangle(32, 440, 960, 150),
        });

        uiEntity.AddBehaviour(new ChoicesPanelBehaviour(font, manager, choicesTex)
        {
            PanelBounds = new Rectangle(32, 280, 500, 160),
        });

        // Entidad NPC que inicia el diálogo al interactuar
        GameEntity npc = World.CreateEntity("Elder", new Vector2(400, 300));
        npc.AddBehaviour(new InteractBehaviour(() =>
            manager.StartDialogue(script)));
    }
}
```

---

## Personalización visual

Los colores y texturas se pueden ajustar después de crear los behaviours:

```csharp
box.BoxColor        = new Color(10, 10, 30, 200); // fondo oscuro semitransparente
box.TextColor       = Color.AntiqueWhite;
box.SpeakerColor    = new Color(255, 200, 50);

choices.PanelColor          = new Color(20, 20, 50, 220);
choices.ChoiceColor         = new Color(180, 180, 200);
choices.SelectedChoiceColor = Color.White;
```

Si se pasa `null` como textura, los behaviours dibujan un rectángulo de color sólido usando un pixel blanco interno.

---

## Extender con lógica propia

Si los behaviours predeterminados no se ajustan a las necesidades del juego, puedes omitirlos y controlar el manager directamente desde un `GameBehaviour` personalizado:

```csharp
public sealed class CustomDialogueBehaviour : GameBehaviour
{
    private readonly DialogueManager _manager;
    private readonly TypewriterEffect _typewriter;
    private KeyboardState _previousKb;

    public CustomDialogueBehaviour(DialogueManager manager)
    {
        _manager    = manager;
        _typewriter = new TypewriterEffect(512);

        _manager.OnLineChanged += line =>
            _typewriter.SetText(LocalizationManager.Get(line.LocalizationKey));
    }

    public override void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _typewriter.Advance(dt);

        KeyboardState kb = Keyboard.GetState();

        if (kb.IsKeyDown(Keys.Space) && _previousKb.IsKeyUp(Keys.Space))
        {
            if (!_typewriter.IsComplete)
                _typewriter.CompleteInstantly();
            else if (_manager.IsActive && !_manager.CurrentLine.HasChoices)
                _manager.Advance();
        }

        // Selección de opciones con teclas numéricas
        if (_manager.IsActive && _manager.CurrentLine.HasChoices)
        {
            for (int i = 0; i < _manager.FilteredChoices.Count && i < 4; i++)
            {
                Keys key = Keys.D1 + i;
                if (kb.IsKeyDown(key) && _previousKb.IsKeyUp(key))
                {
                    _manager.SelectChoice(i);
                    break;
                }
            }
        }

        _previousKb = kb;
    }

    public override void Draw(GameTime gameTime)
    {
        if (!_manager.IsActive) return;

        // Dibujo personalizado con tu propio SpriteBatch/fuente
        SpriteBatch.DrawString(Font, _typewriter.CurrentText,
            new Vector2(64, 440), Color.White);
    }
}
```

---

## Ver también

- [Visión general →](overview.md)
- [DialogueManager →](manager.md)
- [TypewriterEffect →](typewriter.md)
