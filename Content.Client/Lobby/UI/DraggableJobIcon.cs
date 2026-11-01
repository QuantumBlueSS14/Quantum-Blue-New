using System.Numerics;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client.Lobby.UI;

public sealed class DraggableJobIcon : TextureRect
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IUserInterfaceManager _uiManager = default!;

    private const float DefaultScale = 3;
    private const float DefaultHighScale = 8;

    public JobPrototype JobProto { get; private init; }

    private Control? _oldParent;
    private Vector2? _oldScale;
    public bool Dragging => _oldParent is not null;

    public event Action<GUIBoundKeyEventArgs>? OnMouseDown;
    public event Action<Vector2>? OnMouseUp;
    public event Action<Vector2>? OnMouseMove;
    public event Action? OnPriorityChanged;

    public delegate bool CheckCanDrag();
    private readonly CheckCanDrag? _canDragFunc;

    public DraggableJobIcon(JobPrototype jobPrototype, CheckCanDrag? checkDrag = null, TooltipSupplier? tooltipSupplier = null)
    {
        IoCManager.InjectDependencies(this);

        JobProto = jobPrototype;
        _canDragFunc = checkDrag;

        var sprite = _entManager.System<SpriteSystem>();
        var iconProto = _prototypeManager.Index(jobPrototype.Icon);

        Texture = sprite.Frame0(iconProto.Icon);
        TextureScale = new Vector2(DefaultScale);
        VerticalAlignment = VAlignment.Center;
        HorizontalAlignment = HAlignment.Center;
        MouseFilter = MouseFilterMode.Pass;

        if (tooltipSupplier is not null)
            TooltipSupplier = obj => Dragging ? null : tooltipSupplier(obj);
    }

    private void StopDragging()
    {
        if (Parent == _uiManager.PopupRoot)
        {
            Orphan();
            _oldParent?.AddChild(this);
            if (_oldScale is not null)
                TextureScale = _oldScale.Value;
        }

        if (Parent != _oldParent)
            OnPriorityChanged?.Invoke();

        _oldParent = null;
        _oldScale = null;
    }

    private void StartDragging()
    {
        _oldParent = Parent;
        _oldScale = TextureScale;
        Orphan();
        _uiManager.PopupRoot.AddChild(this);
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        if (!Dragging)
            return;

        var mousePos = _uiManager.MousePositionScaled.Position;
        LayoutContainer.SetPosition(this, mousePos - Size / 2.0f);
        OnMouseMove?.Invoke(mousePos);
    }

    protected override void KeyBindDown(GUIBoundKeyEventArgs args)
    {
        base.KeyBindDown(args);
        if (args.Function == EngineKeyFunctions.UIClick && _canDragFunc is not null && _canDragFunc())
        {
            StartDragging();
            OnMouseDown?.Invoke(args);
        }
    }

    protected override void KeyBindUp(GUIBoundKeyEventArgs args)
    {
        base.KeyBindUp(args);
        if (args.Function == EngineKeyFunctions.UIClick)
        {
            OnMouseUp?.Invoke(_uiManager.MousePositionScaled.Position);
            StopDragging();
        }
    }

    public void SetScale(JobPriority priority)
    {
        SetScale(priority == JobPriority.High ? DefaultHighScale : DefaultScale);
    }

    private void SetScale(float scale)
    {
        TextureScale = new Vector2(scale);
    }
}
