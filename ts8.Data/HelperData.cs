using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ts8.Data {
    public class HelperData {
        private static Random random = new Random(Guid.NewGuid().GetHashCode());

        public static int RandomInt(int min, int max) {
            return random.Next(min, max);
        }
    }
}
