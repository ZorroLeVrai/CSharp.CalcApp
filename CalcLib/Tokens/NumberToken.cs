using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CalcLib.Tokens;

internal class NumberToken : ICalcToken
{
    public double Val { get; }

    public NumberToken(double content)
    {
        Val = content;
    }

    public override string ToString() => Val.ToString();
}
