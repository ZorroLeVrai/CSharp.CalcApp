using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CalcLib.Tokens;

internal class Minus: ICalcToken
{
    public override string ToString() => "-";
}
