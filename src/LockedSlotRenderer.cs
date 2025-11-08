using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Cairo;

namespace VintageEssentials
{
    /// <summary>
    /// Renders visual overlays on locked inventory slots
    /// </summary>
    public class LockedSlotRenderer
    {
        private ICoreClientAPI capi;
        private LockedSlotsManager lockedSlotsManager;

        public LockedSlotRenderer(ICoreClientAPI capi, LockedSlotsManager lockedSlotsManager)
        {
            this.capi = capi;
            this.lockedSlotsManager = lockedSlotsManager;
        }

        /// <summary>
        /// Draws a locked slot overlay with 45-degree diagonal lines
        /// </summary>
        public void DrawLockedSlotOverlay(Context ctx, double x, double y, double width, double height)
        {
            ctx.Save();

            // Set up the drawing style for semi-transparent lines
            ctx.SetSourceRGBA(1.0, 1.0, 0.0, 0.4); // Yellow color with 40% opacity
            ctx.LineWidth = 2.0;

            // Draw diagonal lines at 45 degrees
            int spacing = 8; // Space between lines
            
            // Draw lines from bottom-left to top-right
            for (double offset = -height; offset < width; offset += spacing)
            {
                double startX = x + offset;
                double startY = y + height;
                double endX = x + offset + height;
                double endY = y;

                ctx.MoveTo(Math.Max(startX, x), startY - Math.Max(0, x - startX));
                ctx.LineTo(Math.Min(endX, x + width), Math.Max(endY, endY + (x + width - endX)));
            }
            
            ctx.Stroke();

            // Draw a border around the locked slot
            ctx.SetSourceRGBA(1.0, 1.0, 0.0, 0.6); // Slightly more opaque yellow for border
            ctx.LineWidth = 1.5;
            ctx.Rectangle(x, y, width, height);
            ctx.Stroke();

            ctx.Restore();
        }

        /// <summary>
        /// Creates a texture for a locked slot overlay that can be rendered
        /// </summary>
        public LoadedTexture CreateLockedSlotTexture(int width, int height)
        {
            ImageSurface surface = new ImageSurface(Format.Argb32, width, height);
            Context ctx = new Context(surface);
            
            DrawLockedSlotOverlay(ctx, 0, 0, width, height);
            
            LoadedTexture texture = new LoadedTexture(capi)
            {
                Width = width,
                Height = height
            };

            capi.Gui.LoadOrUpdateCairoTexture(surface, false, ref texture);
            
            ctx.Dispose();
            surface.Dispose();
            
            return texture;
        }
    }
}
