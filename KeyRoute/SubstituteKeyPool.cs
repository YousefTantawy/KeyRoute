using System.Windows.Forms;

namespace KeyRoute
{
    public static class SubstituteKeyPool
    {
        public static readonly Keys[] All =
        {
            Keys.F13, Keys.F14, Keys.F15, Keys.F16, Keys.F17, Keys.F18,
            Keys.F19, Keys.F20, Keys.F21, Keys.F22, Keys.F23, Keys.F24
        };

        public static Keys? NextAvailable(IEnumerable<KeyMapping> existing)
        {
            var used = new HashSet<Keys>(existing.Select(m => m.SubstituteKey));
            foreach (var key in All)
            {
                if (!used.Contains(key))
                    return key;
            }
            return null;
        }
    }
}
