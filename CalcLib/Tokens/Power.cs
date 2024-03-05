using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CalcLib.Tokens;

internal class Power : ICalcToken
{
    public override string ToString() => "^";
}
