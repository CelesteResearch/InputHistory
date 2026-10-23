// Modified for the shared runtime: input, rendering, and lifecycle integration.

using Celeste.Core.Rendering;

namespace Celeste.Mods.InputHistory.History
{
    public interface InputEvent
    {
        float Render(ISourceRenderContext context, object owner, float x, float y, float fontSize);
        bool Extends(InputEvent original, bool tas);
        string ToTasString();
        bool HasInput();
    }
}
