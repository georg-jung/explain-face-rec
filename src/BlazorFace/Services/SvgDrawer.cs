using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SixLabors.ImageSharp;

namespace BlazorFace.Services
{
    internal static class SvgDrawer
    {
        public static string DrawPoints(Rectangle viewbox, double pointSize, IEnumerable<PointF> points, string additionalSvgAttributes)
        {
            var halfPointSize = pointSize / 2;
            var sb = new StringBuilder();
            foreach (var p in points)
            {
                sb.Append(FormattableString.Invariant(@$"
<rect
  style=""vector-effect:non-scaling-stroke;fill:currentColor;fill-opacity:1""
  width=""{pointSize}""
  height=""{pointSize}""
  x=""{p.X - halfPointSize}""
  y=""{p.Y - halfPointSize}"" />"));
            }

            var svg = FormattableString.Invariant(@$"
<svg
  viewBox=""{viewbox.X} {viewbox.Y} {viewbox.Width} {viewbox.Height}""
  version=""1.1""
  xmlns=""http://www.w3.org/2000/svg""
  xmlns:svg=""http://www.w3.org/2000/svg""
  {additionalSvgAttributes}>
  <g>
    {sb}
  </g>
</svg>");
            return svg;
        }
    }
}
