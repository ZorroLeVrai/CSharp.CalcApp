namespace Calc.Lib.Tests;

public class ParserTests
{
    private static NumberToken Num(double value) => new(value);

    private static OperatorToken Op(Operator op) => new(op);

    private static NumberExpr N(double value) => new(value);

    [Fact]
    public void Parse_SingleNumber()
    {
        var expr = Parser.Parse([Num(6)]);

        Assert.Equal(N(6), expr);
    }

    [Theory]
    [InlineData(Operator.Add)]
    [InlineData(Operator.Sub)]
    [InlineData(Operator.Mul)]
    [InlineData(Operator.Div)]
    [InlineData(Operator.Pow)]
    [InlineData(Operator.Mod)]
    public void Parse_SingleOperation(Operator op)
    {
        var expr = Parser.Parse([Num(6), Op(op), Num(2)]);

        Assert.Equal(new BinaryExpr(op, N(6), N(2)), expr);
    }

    [Fact]
    public void Parse_EmptyTokenList_Throws()
    {
        var ex = Assert.Throws<CalcException>(() => Parser.Parse([]));

        Assert.Equal(CalcErrorKind.UnexpectedEnd, ex.Kind);
    }

    [Theory]
    [InlineData(Operator.Add)]
    [InlineData(Operator.Mul)]
    [InlineData(Operator.Pow)]
    public void Parse_DanglingOperator_Throws(Operator op)
    {
        var ex = Assert.Throws<CalcException>(() => Parser.Parse([Num(6), Op(op)]));

        Assert.Equal(CalcErrorKind.UnexpectedEnd, ex.Kind);
    }

    [Fact]
    public void Parse_TwoNumbersInARow_Throws()
    {
        var ex = Assert.Throws<CalcException>(() => Parser.Parse([Num(6), Num(2)]));

        Assert.Equal(CalcErrorKind.UnexpectedToken, ex.Kind);
    }

    [Theory]
    [InlineData(Operator.Mul)]
    [InlineData(Operator.Div)]
    [InlineData(Operator.Pow)]
    [InlineData(Operator.Mod)]
    public void Parse_OperatorFirst_Throws(Operator op)
    {
        var ex = Assert.Throws<CalcException>(() => Parser.Parse([Op(op), Num(2)]));

        Assert.Equal(CalcErrorKind.UnexpectedToken, ex.Kind);
    }

    [Fact]
    public void Parse_TwoOperatorsInARow_Throws()
    {
        var ex = Assert.Throws<CalcException>(() => Parser.Parse([Num(6), Op(Operator.Mul), Op(Operator.Div), Num(2)]));

        Assert.Equal(CalcErrorKind.UnexpectedToken, ex.Kind);
    }

    [Fact]
    public void Parse_OnlyAnOperator_Throws()
    {
        var ex = Assert.Throws<CalcException>(() => Parser.Parse([Op(Operator.Add)]));

        Assert.Equal(CalcErrorKind.UnexpectedToken, ex.Kind);
    }

    private static BinaryExpr B(Operator op, Expr left, Expr right) => new(op, left, right);

    [Fact]
    public void Parse_MultiplicationBindsTighterThanAddition_OnTheRight()
    {
        // 2+3*4
        var expr = Parser.Parse([Num(2), Op(Operator.Add), Num(3), Op(Operator.Mul), Num(4)]);

        Assert.Equal(B(Operator.Add, N(2), B(Operator.Mul, N(3), N(4))), expr);
    }

    [Fact]
    public void Parse_MultiplicationBindsTighterThanAddition_OnTheLeft()
    {
        // 2*3+4
        var expr = Parser.Parse([Num(2), Op(Operator.Mul), Num(3), Op(Operator.Add), Num(4)]);

        Assert.Equal(B(Operator.Add, B(Operator.Mul, N(2), N(3)), N(4)), expr);
    }

    [Fact]
    public void Parse_DivisionBindsTighterThanSubtraction()
    {
        // 10-6/3
        var expr = Parser.Parse([Num(10), Op(Operator.Sub), Num(6), Op(Operator.Div), Num(3)]);

        Assert.Equal(B(Operator.Sub, N(10), B(Operator.Div, N(6), N(3))), expr);
    }

    [Fact]
    public void Parse_SubtractionIsLeftAssociative()
    {
        // 8-3-2
        var expr = Parser.Parse([Num(8), Op(Operator.Sub), Num(3), Op(Operator.Sub), Num(2)]);

        Assert.Equal(B(Operator.Sub, B(Operator.Sub, N(8), N(3)), N(2)), expr);
    }

    [Fact]
    public void Parse_DivisionIsLeftAssociative()
    {
        // 16/4/2
        var expr = Parser.Parse([Num(16), Op(Operator.Div), Num(4), Op(Operator.Div), Num(2)]);

        Assert.Equal(B(Operator.Div, B(Operator.Div, N(16), N(4)), N(2)), expr);
    }

    [Fact]
    public void Parse_AdditionAndSubtractionShareALevel_LeftToRight()
    {
        // 1-2+3
        var expr = Parser.Parse([Num(1), Op(Operator.Sub), Num(2), Op(Operator.Add), Num(3)]);

        Assert.Equal(B(Operator.Add, B(Operator.Sub, N(1), N(2)), N(3)), expr);
    }

    [Fact]
    public void Parse_MultiplicationAndDivisionShareALevel_LeftToRight()
    {
        // 2*6/3
        var expr = Parser.Parse([Num(2), Op(Operator.Mul), Num(6), Op(Operator.Div), Num(3)]);

        Assert.Equal(B(Operator.Div, B(Operator.Mul, N(2), N(6)), N(3)), expr);
    }

    [Fact]
    public void Parse_PowerIsRightAssociative()
    {
        // 2^3^2
        var expr = Parser.Parse([Num(2), Op(Operator.Pow), Num(3), Op(Operator.Pow), Num(2)]);

        Assert.Equal(B(Operator.Pow, N(2), B(Operator.Pow, N(3), N(2))), expr);
    }

    [Fact]
    public void Parse_PowerBindsTighterThanMultiplication_OnTheRight()
    {
        // 2*3^2
        var expr = Parser.Parse([Num(2), Op(Operator.Mul), Num(3), Op(Operator.Pow), Num(2)]);

        Assert.Equal(B(Operator.Mul, N(2), B(Operator.Pow, N(3), N(2))), expr);
    }

    [Fact]
    public void Parse_PowerBindsTighterThanMultiplication_OnTheLeft()
    {
        // 2^3*4
        var expr = Parser.Parse([Num(2), Op(Operator.Pow), Num(3), Op(Operator.Mul), Num(4)]);

        Assert.Equal(B(Operator.Mul, B(Operator.Pow, N(2), N(3)), N(4)), expr);
    }

    [Fact]
    public void Parse_AllLevelsTogether()
    {
        // 1+2*3^4^5-6
        var expr = Parser.Parse(
        [
            Num(1), Op(Operator.Add), Num(2), Op(Operator.Mul), Num(3), Op(Operator.Pow),
            Num(4), Op(Operator.Pow), Num(5), Op(Operator.Sub), Num(6),
        ]);

        var expected = B(
            Operator.Sub,
            B(
                Operator.Add,
                N(1),
                B(Operator.Mul, N(2), B(Operator.Pow, N(3), B(Operator.Pow, N(4), N(5))))),
            N(6));
        Assert.Equal(expected, expr);
    }

    private static IdentifierToken Id(string name) => new(name);

    private static ConstantExpr Pi => new("pi", Math.PI);

    private static ConstantExpr E => new("e", Math.E);

    [Fact]
    public void Parse_ConstantPi()
    {
        Assert.Equal(Pi, Parser.Parse([Id("pi")]));
    }

    [Fact]
    public void Parse_ConstantE()
    {
        Assert.Equal(E, Parser.Parse([Id("e")]));
    }

    [Fact]
    public void Parse_ConstantsHaveExactValues()
    {
        Assert.Equal(Math.PI, Assert.IsType<ConstantExpr>(Parser.Parse([Id("pi")])).Value);
        Assert.Equal(Math.E, Assert.IsType<ConstantExpr>(Parser.Parse([Id("e")])).Value);
    }

    [Fact]
    public void Parse_ConstantAsAnOperand()
    {
        // 2*pi
        var expr = Parser.Parse([Num(2), Op(Operator.Mul), Id("pi")]);

        Assert.Equal(B(Operator.Mul, N(2), Pi), expr);
    }

    [Fact]
    public void Parse_ConstantsOnBothSides()
    {
        // pi+e
        var expr = Parser.Parse([Id("pi"), Op(Operator.Add), Id("e")]);

        Assert.Equal(B(Operator.Add, Pi, E), expr);
    }

    [Fact]
    public void Parse_ConstantsFollowOperatorPrecedence()
    {
        // 1+pi*e^2
        var expr = Parser.Parse(
            [Num(1), Op(Operator.Add), Id("pi"), Op(Operator.Mul), Id("e"), Op(Operator.Pow), Num(2)]);

        Assert.Equal(B(Operator.Add, N(1), B(Operator.Mul, Pi, B(Operator.Pow, E, N(2)))), expr);
    }

    [Fact]
    public void Parse_NegatedConstant()
    {
        // -pi
        var expr = Parser.Parse([Op(Operator.Sub), Id("pi")]);

        Assert.Equal(Neg(Pi), expr);
    }

    [Fact]
    public void Parse_ConstantInTheExponent()
    {
        // 2^pi
        var expr = Parser.Parse([Num(2), Op(Operator.Pow), Id("pi")]);

        Assert.Equal(B(Operator.Pow, N(2), Pi), expr);
    }

    [Fact]
    public void Parse_ParenthesisedConstant()
    {
        var expr = Parser.Parse([LParen(), Id("pi"), RParen()]);

        Assert.Equal(Pi, expr);
    }

    [Theory]
    [InlineData("x")]
    [InlineData("foo")]
    [InlineData("PI")]
    [InlineData("Pi")]
    [InlineData("E")]
    [InlineData("pie")]
    [InlineData("pi2")]
    [InlineData("e5")]
    public void Parse_UnknownName_Throws(string name)
    {
        var ex = Assert.Throws<CalcException>(() => Parser.Parse([Id(name)]));

        Assert.Equal(CalcErrorKind.UnknownIdentifier, ex.Kind);
        Assert.Contains($"'{name}'", ex.Message);
    }

    [Fact]
    public void Parse_UnknownNameInsideAnExpression_Throws()
    {
        // 1+(2*foo)
        var ex = Assert.Throws<CalcException>(
            () => Parser.Parse([Num(1), Op(Operator.Add), LParen(), Num(2), Op(Operator.Mul), Id("foo"), RParen()]));

        Assert.Equal(CalcErrorKind.UnknownIdentifier, ex.Kind);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Parse_ConstantNextToANumber_Throws(bool numberFirst)
    {
        // 2pi and pi2 as tokens: implicit multiplication is not supported
        Token[] tokens = numberFirst ? [Num(2), Id("pi")] : [Id("pi"), Num(2)];

        var ex = Assert.Throws<CalcException>(() => Parser.Parse(tokens));

        Assert.Equal(CalcErrorKind.UnexpectedToken, ex.Kind);
    }

    [Fact]
    public void Parse_TwoConstantsInARow_Throws()
    {
        // pi e
        var ex = Assert.Throws<CalcException>(() => Parser.Parse([Id("pi"), Id("e")]));

        Assert.Equal(CalcErrorKind.UnexpectedToken, ex.Kind);
    }

    [Fact]
    public void Parse_ConstantBeforeAnOpeningParenthesis_Throws()
    {
        // pi(2)
        var ex = Assert.Throws<CalcException>(() => Parser.Parse([Id("pi"), LParen(), Num(2), RParen()]));

        Assert.Equal(CalcErrorKind.UnexpectedToken, ex.Kind);
    }

    [Fact]
    public void Parse_DanglingOperatorAfterAConstant_Throws()
    {
        // pi+
        var ex = Assert.Throws<CalcException>(() => Parser.Parse([Id("pi"), Op(Operator.Add)]));

        Assert.Equal(CalcErrorKind.UnexpectedEnd, ex.Kind);
    }

    [Fact]
    public void Parse_LongChainOfConstants_Works()
    {
        var tokens = new List<Token> { Id("pi") };
        for (var i = 0; i < TestConstants.MaxDepthLimit; i++)
        {
            tokens.Add(Op(Operator.Add));
            tokens.Add(Id("e"));
        }

        var expr = Parser.Parse(tokens);

        Assert.Equal(TestConstants.MaxDepthLimit + 1, CountNumbers(expr));
    }

    private static CallExpr Call(string name, Expr argument) => new(name, argument);

    private static List<Token> NestedCalls(int depth, string name = "abs", double num = 0)
    {
        var tokens = new List<Token>();
        for (var i = 0; i < depth; i++)
        {
            tokens.Add(Id(name));
            tokens.Add(LParen());
        }

        tokens.Add(Num(num));
        tokens.AddRange(Enumerable.Repeat<Token>(RParen(), depth));
        return tokens;
    }

    [Fact]
    public void Parse_FunctionCall()
    {
        // sqrt(16)
        var expr = Parser.Parse([Id("sqrt"), LParen(), Num(16), RParen()]);

        Assert.Equal(Call("sqrt", N(16)), expr);
    }

    [Theory]
    [InlineData("sqrt")]
    [InlineData("abs")]
    [InlineData("exp")]
    [InlineData("ln")]
    [InlineData("log")]
    [InlineData("log2")]
    [InlineData("sin")]
    [InlineData("cos")]
    [InlineData("tan")]
    [InlineData("asin")]
    [InlineData("acos")]
    [InlineData("atan")]
    [InlineData("floor")]
    [InlineData("ceil")]
    [InlineData("round")]
    public void Parse_EveryKnownFunction(string name)
    {
        var expr = Parser.Parse([Id(name), LParen(), Num(1), RParen()]);

        Assert.Equal(Call(name, N(1)), expr);
    }

    [Fact]
    public void Parse_FunctionWithAnExpressionArgument()
    {
        // sqrt(2+3*4)
        var expr = Parser.Parse(
            [Id("sqrt"), LParen(), Num(2), Op(Operator.Add), Num(3), Op(Operator.Mul), Num(4), RParen()]);

        Assert.Equal(Call("sqrt", B(Operator.Add, N(2), B(Operator.Mul, N(3), N(4)))), expr);
    }

    [Fact]
    public void Parse_FunctionResultUsedInAnExpression()
    {
        // sqrt(2+2)*3
        var expr = Parser.Parse(
            [Id("sqrt"), LParen(), Num(2), Op(Operator.Add), Num(2), RParen(), Op(Operator.Mul), Num(3)]);

        Assert.Equal(B(Operator.Mul, Call("sqrt", B(Operator.Add, N(2), N(2))), N(3)), expr);
    }

    [Fact]
    public void Parse_FunctionAsTheRightOperand()
    {
        // 1+sqrt(4)
        var expr = Parser.Parse([Num(1), Op(Operator.Add), Id("sqrt"), LParen(), Num(4), RParen()]);

        Assert.Equal(B(Operator.Add, N(1), Call("sqrt", N(4))), expr);
    }

    [Fact]
    public void Parse_NegatedFunction()
    {
        // -sqrt(4)
        var expr = Parser.Parse([Op(Operator.Sub), Id("sqrt"), LParen(), Num(4), RParen()]);

        Assert.Equal(Neg(Call("sqrt", N(4))), expr);
    }

    [Fact]
    public void Parse_NegatedFunctionRaisedToAPower_NegatesTheWholePower()
    {
        // -sqrt(4)^2 is -(sqrt(4)^2)
        var expr = Parser.Parse(
            [Op(Operator.Sub), Id("sqrt"), LParen(), Num(4), RParen(), Op(Operator.Pow), Num(2)]);

        Assert.Equal(Neg(B(Operator.Pow, Call("sqrt", N(4)), N(2))), expr);
    }

    [Fact]
    public void Parse_FunctionInTheBaseAndTheExponent()
    {
        // sqrt(4)^sqrt(9)
        var expr = Parser.Parse(
        [
            Id("sqrt"), LParen(), Num(4), RParen(), Op(Operator.Pow),
            Id("sqrt"), LParen(), Num(9), RParen(),
        ]);

        Assert.Equal(B(Operator.Pow, Call("sqrt", N(4)), Call("sqrt", N(9))), expr);
    }

    [Fact]
    public void Parse_NestedFunctions()
    {
        // sqrt(abs(16))
        var expr = Parser.Parse([Id("sqrt"), LParen(), Id("abs"), LParen(), Num(16), RParen(), RParen()]);

        Assert.Equal(Call("sqrt", Call("abs", N(16))), expr);
    }

    [Fact]
    public void Parse_FunctionOfAConstant()
    {
        // sin(pi)
        var expr = Parser.Parse([Id("sin"), LParen(), Id("pi"), RParen()]);

        Assert.Equal(Call("sin", Pi), expr);
    }

    [Fact]
    public void Parse_FunctionOfANegativeNumber()
    {
        // abs(-3)
        var expr = Parser.Parse([Id("abs"), LParen(), Op(Operator.Sub), Num(3), RParen()]);

        Assert.Equal(Call("abs", Neg(N(3))), expr);
    }

    [Fact]
    public void Parse_FunctionWithParenthesisedArgument()
    {
        // sqrt((4))
        var expr = Parser.Parse([Id("sqrt"), LParen(), LParen(), Num(4), RParen(), RParen()]);

        Assert.Equal(Call("sqrt", N(4)), expr);
    }

    [Fact]
    public void Parse_ParenthesisedFunction()
    {
        // (sqrt(4))
        var expr = Parser.Parse([LParen(), Id("sqrt"), LParen(), Num(4), RParen(), RParen()]);

        Assert.Equal(Call("sqrt", N(4)), expr);
    }

    [Fact]
    public void Parse_FunctionBeforeModulo()
    {
        // sqrt(16)%3
        var expr = Parser.Parse([Id("sqrt"), LParen(), Num(16), RParen(), Op(Operator.Mod), Num(3)]);

        Assert.Equal(B(Operator.Mod, Call("sqrt", N(16)), N(3)), expr);
    }

    [Theory]
    [InlineData("foo")]
    [InlineData("Sqrt")]
    [InlineData("SQRT")]
    [InlineData("sqr")]
    [InlineData("sqrt2")]
    public void Parse_UnknownFunction_Throws(string name)
    {
        var ex = Assert.Throws<CalcException>(() => Parser.Parse([Id(name), LParen(), Num(2), RParen()]));

        Assert.Equal(CalcErrorKind.UnknownIdentifier, ex.Kind);
        Assert.Contains($"'{name}'", ex.Message);
    }

    [Theory]
    [InlineData("pi")]
    [InlineData("e")]
    public void Parse_ConstantCalledLikeAFunction_Throws(string name)
    {
        var ex = Assert.Throws<CalcException>(() => Parser.Parse([Id(name), LParen(), Num(2), RParen()]));

        Assert.Equal(CalcErrorKind.UnexpectedToken, ex.Kind);
    }

    [Fact]
    public void Parse_Log2AndLogAreDifferentFunctions()
    {
        // log2(8)*log(100)
        var expr = Parser.Parse(
        [
            Id("log2"), LParen(), Num(8), RParen(), Op(Operator.Mul),
            Id("log"), LParen(), Num(100), RParen(),
        ]);

        Assert.Equal(B(Operator.Mul, Call("log2", N(8)), Call("log", N(100))), expr);
    }

    [Fact]
    public void Parse_Log2WithoutParentheses_Throws()
    {
        // log2 8
        var ex = Assert.Throws<CalcException>(() => Parser.Parse([Id("log2"), Num(8)]));

        Assert.Equal(CalcErrorKind.UnexpectedToken, ex.Kind);
    }

    [Fact]
    public void Parse_LogFollowedByANumber_IsNotLog2()
    {
        // log 2 is the function log without its parentheses, not log2
        var ex = Assert.Throws<CalcException>(() => Parser.Parse([Id("log"), Num(2)]));

        Assert.Equal(CalcErrorKind.UnexpectedToken, ex.Kind);
    }

    [Fact]
    public void Parse_FunctionNameAloneAtTheEnd_Throws()
    {
        // sqrt
        var ex = Assert.Throws<CalcException>(() => Parser.Parse([Id("sqrt")]));

        Assert.Equal(CalcErrorKind.UnexpectedEnd, ex.Kind);
    }

    [Fact]
    public void Parse_FunctionNameWithoutParentheses_Throws()
    {
        // sqrt 4
        var ex = Assert.Throws<CalcException>(() => Parser.Parse([Id("sqrt"), Num(4)]));

        Assert.Equal(CalcErrorKind.UnexpectedToken, ex.Kind);
    }

    [Fact]
    public void Parse_FunctionNameInsideAnExpressionWithoutParentheses_Throws()
    {
        // sqrt+1
        var ex = Assert.Throws<CalcException>(() => Parser.Parse([Id("sqrt"), Op(Operator.Add), Num(1)]));

        Assert.Equal(CalcErrorKind.UnexpectedToken, ex.Kind);
    }

    [Fact]
    public void Parse_FunctionWithAnEmptyArgument_Throws()
    {
        // sqrt()
        var ex = Assert.Throws<CalcException>(() => Parser.Parse([Id("sqrt"), LParen(), RParen()]));

        Assert.Equal(CalcErrorKind.UnexpectedToken, ex.Kind);
    }

    [Fact]
    public void Parse_FunctionWithAnOpeningParenthesisAtTheEnd_Throws()
    {
        // sqrt(
        var ex = Assert.Throws<CalcException>(() => Parser.Parse([Id("sqrt"), LParen()]));

        Assert.Equal(CalcErrorKind.UnexpectedEnd, ex.Kind);
    }

    [Fact]
    public void Parse_FunctionMissingItsClosingParenthesis_Throws()
    {
        // sqrt(4
        var ex = Assert.Throws<CalcException>(() => Parser.Parse([Id("sqrt"), LParen(), Num(4)]));

        Assert.Equal(CalcErrorKind.UnbalancedParentheses, ex.Kind);
    }

    [Fact]
    public void Parse_FunctionWithAnExtraClosingParenthesis_Throws()
    {
        // sqrt(4))
        var ex = Assert.Throws<CalcException>(
            () => Parser.Parse([Id("sqrt"), LParen(), Num(4), RParen(), RParen()]));

        Assert.Equal(CalcErrorKind.UnbalancedParentheses, ex.Kind);
    }

    [Fact]
    public void Parse_FunctionWithADanglingOperatorInItsArgument_Throws()
    {
        // sqrt(4+)
        var ex = Assert.Throws<CalcException>(
            () => Parser.Parse([Id("sqrt"), LParen(), Num(4), Op(Operator.Add), RParen()]));

        Assert.Equal(CalcErrorKind.UnexpectedToken, ex.Kind);
    }

    [Fact]
    public void Parse_FunctionWithTwoNumbersInItsArgument_Throws()
    {
        // sqrt(4 4)
        var ex = Assert.Throws<CalcException>(
            () => Parser.Parse([Id("sqrt"), LParen(), Num(4), Num(4), RParen()]));

        Assert.Equal(CalcErrorKind.UnexpectedToken, ex.Kind);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Parse_FunctionNextToAnOperandWithoutAnOperator_Throws(bool numberFirst)
    {
        // 2sqrt(4) and sqrt(4)2
        Token[] call = [Id("sqrt"), LParen(), Num(4), RParen()];
        Token[] tokens = numberFirst ? [Num(2), .. call] : [.. call, Num(2)];

        var ex = Assert.Throws<CalcException>(() => Parser.Parse(tokens));

        Assert.Equal(CalcErrorKind.UnexpectedToken, ex.Kind);
    }

    [Fact]
    public void Parse_TwoFunctionsInARow_Throws()
    {
        // sqrt(4)sqrt(4)
        Token[] call = [Id("sqrt"), LParen(), Num(4), RParen()];

        var ex = Assert.Throws<CalcException>(() => Parser.Parse([.. call, .. call]));

        Assert.Equal(CalcErrorKind.UnexpectedToken, ex.Kind);
    }

    [Fact]
    public void Parse_UnknownFunctionInsideAnExpression_Throws()
    {
        // 1+foo(2)*3
        var ex = Assert.Throws<CalcException>(
            () => Parser.Parse([Num(1), Op(Operator.Add), Id("foo"), LParen(), Num(2), RParen()]));

        Assert.Equal(CalcErrorKind.UnknownIdentifier, ex.Kind);
    }

    [Fact]
    public void Parse_NestedCallsUpToTheLimit_Work()
    {
        var expr = Parser.Parse(NestedCalls(TestConstants.MaxDepthLimit));

        var depth = 0;
        while (expr is CallExpr call)
        {
            depth++;
            expr = call.Argument;
        }

        Assert.Equal(TestConstants.MaxDepthLimit, depth);
        Assert.Equal(N(0), expr);
    }

    [Fact]
    public void Parse_NestedCallsBeyondTheLimit_AreTooDeep()
    {
        var ex = Assert.Throws<CalcException>(() => Parser.Parse(NestedCalls(TestConstants.MaxDepthLimit + 1)));

        Assert.Equal(CalcErrorKind.ExpressionTooDeep, ex.Kind);
    }

    [Fact]
    public void Parse_HugeNumberOfNestedCalls_IsRejectedWithoutOverflowingTheStack()
    {
        var ex = Assert.Throws<CalcException>(() => Parser.Parse(NestedCalls(200_000)));

        Assert.Equal(CalcErrorKind.ExpressionTooDeep, ex.Kind);
    }

    [Fact]
    public void Parse_NestedCallsMixedWithParenthesesAndNegations_CountTowardsTheSameLimit()
    {
        // abs(-(abs(-(...)))): each level adds a call, a negation and a group
        var tokens = new List<Token>();
        const int levels = 400;
        for (var i = 0; i < levels; i++)
        {
            tokens.AddRange([Id("abs"), LParen(), Op(Operator.Sub), LParen()]);
        }

        tokens.Add(Num(1));
        for (var i = 0; i < levels; i++)
        {
            tokens.AddRange([RParen(), RParen()]);
        }

        var ex = Assert.Throws<CalcException>(() => Parser.Parse(tokens));

        Assert.Equal(CalcErrorKind.ExpressionTooDeep, ex.Kind);
    }

    [Fact]
    public void Parse_LongChainOfCalls_Works()
    {
        // abs(1)+abs(1)+...: 600 calls chained with additions
        var tokens = new List<Token>();
        for (var i = 0; i < 600; i++)
        {
            if (i > 0)
            {
                tokens.Add(Op(Operator.Add));
            }

            tokens.AddRange([Id("abs"), LParen(), Num(1), RParen()]);
        }

        var expr = Parser.Parse(tokens);

        Assert.Equal(600, CountNumbers(expr));
    }

    [Fact]
    public void Parse_ModuloBindsTighterThanAddition_OnTheRight()
    {
        // 2+7%4
        var expr = Parser.Parse([Num(2), Op(Operator.Add), Num(7), Op(Operator.Mod), Num(4)]);

        Assert.Equal(B(Operator.Add, N(2), B(Operator.Mod, N(7), N(4))), expr);
    }

    [Fact]
    public void Parse_ModuloBindsTighterThanSubtraction_OnTheLeft()
    {
        // 7%4-2
        var expr = Parser.Parse([Num(7), Op(Operator.Mod), Num(4), Op(Operator.Sub), Num(2)]);

        Assert.Equal(B(Operator.Sub, B(Operator.Mod, N(7), N(4)), N(2)), expr);
    }

    [Fact]
    public void Parse_ModuloIsLeftAssociative()
    {
        // 10%4%3
        var expr = Parser.Parse([Num(10), Op(Operator.Mod), Num(4), Op(Operator.Mod), Num(3)]);

        Assert.Equal(B(Operator.Mod, B(Operator.Mod, N(10), N(4)), N(3)), expr);
    }

    [Fact]
    public void Parse_ModuloSharesALevelWithMultiplication_LeftToRight()
    {
        // 2*7%4 is (2*7)%4
        var expr = Parser.Parse([Num(2), Op(Operator.Mul), Num(7), Op(Operator.Mod), Num(4)]);

        Assert.Equal(B(Operator.Mod, B(Operator.Mul, N(2), N(7)), N(4)), expr);
    }

    [Fact]
    public void Parse_ModuloSharesALevelWithMultiplication_WhenModuloComesFirst()
    {
        // 7%4*2 is (7%4)*2
        var expr = Parser.Parse([Num(7), Op(Operator.Mod), Num(4), Op(Operator.Mul), Num(2)]);

        Assert.Equal(B(Operator.Mul, B(Operator.Mod, N(7), N(4)), N(2)), expr);
    }

    [Fact]
    public void Parse_ModuloSharesALevelWithDivision_LeftToRight()
    {
        // 10/4%3 is (10/4)%3
        var expr = Parser.Parse([Num(10), Op(Operator.Div), Num(4), Op(Operator.Mod), Num(3)]);

        Assert.Equal(B(Operator.Mod, B(Operator.Div, N(10), N(4)), N(3)), expr);
    }

    [Fact]
    public void Parse_PowerBindsTighterThanModulo_OnTheLeft()
    {
        // 2^3%5 is (2^3)%5
        var expr = Parser.Parse([Num(2), Op(Operator.Pow), Num(3), Op(Operator.Mod), Num(5)]);

        Assert.Equal(B(Operator.Mod, B(Operator.Pow, N(2), N(3)), N(5)), expr);
    }

    [Fact]
    public void Parse_PowerBindsTighterThanModulo_OnTheRight()
    {
        // 2%3^2 is 2%(3^2)
        var expr = Parser.Parse([Num(2), Op(Operator.Mod), Num(3), Op(Operator.Pow), Num(2)]);

        Assert.Equal(B(Operator.Mod, N(2), B(Operator.Pow, N(3), N(2))), expr);
    }

    [Fact]
    public void Parse_ModuloWithUnaryMinusOnTheLeft_NegatesOnlyTheLeftOperand()
    {
        // -7%3 is (-7)%3
        var expr = Parser.Parse([Op(Operator.Sub), Num(7), Op(Operator.Mod), Num(3)]);

        Assert.Equal(B(Operator.Mod, Neg(N(7)), N(3)), expr);
    }

    [Fact]
    public void Parse_ModuloWithUnaryMinusOnTheRight()
    {
        // 7%-3
        var expr = Parser.Parse([Num(7), Op(Operator.Mod), Op(Operator.Sub), Num(3)]);

        Assert.Equal(B(Operator.Mod, N(7), Neg(N(3))), expr);
    }

    [Fact]
    public void Parse_ModuloWithParentheses()
    {
        // (2+7)%4
        var expr = Parser.Parse(
            [LParen(), Num(2), Op(Operator.Add), Num(7), RParen(), Op(Operator.Mod), Num(4)]);

        Assert.Equal(B(Operator.Mod, B(Operator.Add, N(2), N(7)), N(4)), expr);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void Parse_DanglingModulo_Throws(int dangling)
    {
        // 7% and 7%%
        var tokens = new List<Token> { Num(7) };
        tokens.AddRange(Enumerable.Repeat<Token>(Op(Operator.Mod), dangling));

        var ex = Assert.Throws<CalcException>(() => Parser.Parse(tokens));

        Assert.Equal(dangling == 1 ? CalcErrorKind.UnexpectedEnd : CalcErrorKind.UnexpectedToken, ex.Kind);
    }

    private static LeftParenToken LParen() => new();

    private static RightParenToken RParen() => new();

    [Fact]
    public void Parse_ParenthesesOverridePrecedence()
    {
        // (2+3)*4
        var expr = Parser.Parse([LParen(), Num(2), Op(Operator.Add), Num(3), RParen(), Op(Operator.Mul), Num(4)]);

        Assert.Equal(B(Operator.Mul, B(Operator.Add, N(2), N(3)), N(4)), expr);
    }

    [Fact]
    public void Parse_ParenthesesOnTheRight()
    {
        // 4*(2+3)
        var expr = Parser.Parse([Num(4), Op(Operator.Mul), LParen(), Num(2), Op(Operator.Add), Num(3), RParen()]);

        Assert.Equal(B(Operator.Mul, N(4), B(Operator.Add, N(2), N(3))), expr);
    }

    [Fact]
    public void Parse_ParenthesesAroundASingleNumber()
    {
        var expr = Parser.Parse([LParen(), Num(5), RParen()]);

        Assert.Equal(N(5), expr);
    }

    [Fact]
    public void Parse_RedundantParentheses()
    {
        // ((2+3))*4
        var expr = Parser.Parse(
            [LParen(), LParen(), Num(2), Op(Operator.Add), Num(3), RParen(), RParen(), Op(Operator.Mul), Num(4)]);

        Assert.Equal(B(Operator.Mul, B(Operator.Add, N(2), N(3)), N(4)), expr);
    }

    [Fact]
    public void Parse_NestedParentheses()
    {
        // 2*(3+(4-1))
        var expr = Parser.Parse(
        [
            Num(2), Op(Operator.Mul), LParen(), Num(3), Op(Operator.Add),
            LParen(), Num(4), Op(Operator.Sub), Num(1), RParen(), RParen(),
        ]);

        Assert.Equal(B(Operator.Mul, N(2), B(Operator.Add, N(3), B(Operator.Sub, N(4), N(1)))), expr);
    }

    [Fact]
    public void Parse_ParenthesesOverrideRightAssociativityOfPower()
    {
        // (2^3)^2
        var expr = Parser.Parse(
            [LParen(), Num(2), Op(Operator.Pow), Num(3), RParen(), Op(Operator.Pow), Num(2)]);

        Assert.Equal(B(Operator.Pow, B(Operator.Pow, N(2), N(3)), N(2)), expr);
    }

    [Fact]
    public void Parse_ParenthesesOverrideLeftAssociativityOfSubtraction()
    {
        // 8-(3-2)
        var expr = Parser.Parse(
            [Num(8), Op(Operator.Sub), LParen(), Num(3), Op(Operator.Sub), Num(2), RParen()]);

        Assert.Equal(B(Operator.Sub, N(8), B(Operator.Sub, N(3), N(2))), expr);
    }

    [Fact]
    public void Parse_TwoParenthesisedGroups()
    {
        // (1+2)*(3+4)
        var expr = Parser.Parse(
        [
            LParen(), Num(1), Op(Operator.Add), Num(2), RParen(), Op(Operator.Mul),
            LParen(), Num(3), Op(Operator.Add), Num(4), RParen(),
        ]);

        Assert.Equal(
            B(Operator.Mul, B(Operator.Add, N(1), N(2)), B(Operator.Add, N(3), N(4))),
            expr);
    }

    [Fact]
    public void Parse_ModeratelyDeepNesting_Works()
    {
        var expr = Parser.Parse(NestedParentheses(50));

        Assert.Equal(N(0), expr);
    }

    [Fact]
    public void Parse_MissingClosingParenthesis_Throws()
    {
        // (2+3
        var ex = Assert.Throws<CalcException>(
            () => Parser.Parse([LParen(), Num(2), Op(Operator.Add), Num(3)]));

        Assert.Equal(CalcErrorKind.UnbalancedParentheses, ex.Kind);
    }

    [Fact]
    public void Parse_MissingOneOfTwoClosingParentheses_Throws()
    {
        // ((2)
        var ex = Assert.Throws<CalcException>(() => Parser.Parse([LParen(), LParen(), Num(2), RParen()]));

        Assert.Equal(CalcErrorKind.UnbalancedParentheses, ex.Kind);
    }

    [Fact]
    public void Parse_ExtraClosingParenthesis_Throws()
    {
        // 2+3)
        var ex = Assert.Throws<CalcException>(
            () => Parser.Parse([Num(2), Op(Operator.Add), Num(3), RParen()]));

        Assert.Equal(CalcErrorKind.UnbalancedParentheses, ex.Kind);
    }

    [Fact]
    public void Parse_ExtraClosingParenthesisAfterGroup_Throws()
    {
        // (2))
        var ex = Assert.Throws<CalcException>(() => Parser.Parse([LParen(), Num(2), RParen(), RParen()]));

        Assert.Equal(CalcErrorKind.UnbalancedParentheses, ex.Kind);
    }

    [Fact]
    public void Parse_EmptyParentheses_Throws()
    {
        var ex = Assert.Throws<CalcException>(() => Parser.Parse([LParen(), RParen()]));

        Assert.Equal(CalcErrorKind.UnexpectedToken, ex.Kind);
    }

    [Fact]
    public void Parse_OperatorWithoutRightSideBeforeClosingParenthesis_Throws()
    {
        // (2+)
        var ex = Assert.Throws<CalcException>(
            () => Parser.Parse([LParen(), Num(2), Op(Operator.Add), RParen()]));

        Assert.Equal(CalcErrorKind.UnexpectedToken, ex.Kind);
    }

    [Fact]
    public void Parse_LoneClosingParenthesis_Throws()
    {
        var ex = Assert.Throws<CalcException>(() => Parser.Parse([RParen()]));

        Assert.Equal(CalcErrorKind.UnexpectedToken, ex.Kind);
    }

    [Fact]
    public void Parse_LoneOpeningParenthesis_Throws()
    {
        var ex = Assert.Throws<CalcException>(() => Parser.Parse([LParen()]));

        Assert.Equal(CalcErrorKind.UnexpectedEnd, ex.Kind);
    }

    [Fact]
    public void Parse_OperatorThenOpeningParenthesisAtEnd_Throws()
    {
        // 2*(
        var ex = Assert.Throws<CalcException>(() => Parser.Parse([Num(2), Op(Operator.Mul), LParen()]));

        Assert.Equal(CalcErrorKind.UnexpectedEnd, ex.Kind);
    }

    [Fact]
    public void Parse_TwoNumbersInsideParentheses_Throws()
    {
        // (2 3)
        var ex = Assert.Throws<CalcException>(() => Parser.Parse([LParen(), Num(2), Num(3), RParen()]));

        Assert.Equal(CalcErrorKind.UnexpectedToken, ex.Kind);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Parse_AdjacentOperandsWithoutOperator_Throw(bool numberFirst)
    {
        // 2(3) and (2)3: implicit multiplication is not supported
        Token[] tokens = numberFirst
            ? [Num(2), LParen(), Num(3), RParen()]
            : [LParen(), Num(2), RParen(), Num(3)];

        var ex = Assert.Throws<CalcException>(() => Parser.Parse(tokens));

        Assert.Equal(CalcErrorKind.UnexpectedToken, ex.Kind);
    }

    [Fact]
    public void Parse_TrailingOperatorAfterHigherPrecedence_Throws()
    {
        // 2+3*
        var ex = Assert.Throws<CalcException>(
            () => Parser.Parse([Num(2), Op(Operator.Add), Num(3), Op(Operator.Mul)]));

        Assert.Equal(CalcErrorKind.UnexpectedEnd, ex.Kind);
    }

    [Theory]
    [InlineData(Operator.Add)]
    [InlineData(Operator.Sub)]
    [InlineData(Operator.Mul)]
    [InlineData(Operator.Div)]
    [InlineData(Operator.Pow)]
    [InlineData(Operator.Mod)]
    public void Parse_ChainAtTheDepthLimit_DoesNotOverflow(Operator op)
    {
        var tokens = LongChain(TestConstants.MaxDepthLimit, _ => op);

        var expr = Parser.Parse(tokens);

        Assert.Equal(TestConstants.MaxDepthLimit + 1, CountNumbers(expr));
    }

    [Fact]
    public void Parse_MixedOperatorChainAtTheDepthLimit_DoesNotOverflow()
    {
        Operator[] cycle = [Operator.Add, Operator.Sub, Operator.Mul, Operator.Div, Operator.Pow, Operator.Mod];
        var tokens = LongChain(TestConstants.MaxDepthLimit, i => cycle[i % cycle.Length]);

        var expr = Parser.Parse(tokens);

        Assert.Equal(TestConstants.MaxDepthLimit + 1, CountNumbers(expr));
    }

    [Fact]
    public void Parse_LongChainViaTokenizer_DoesNotOverflow()
    {
        var input = string.Join("+", Enumerable.Repeat("1", TestConstants.MaxDepthLimit + 1));

        var expr = Parser.Parse(Tokenizer.Tokenize(input));

        Assert.Equal(TestConstants.MaxDepthLimit + 1, CountNumbers(expr));
    }

    [Theory]
    [InlineData(Operator.Add)]
    [InlineData(Operator.Sub)]
    [InlineData(Operator.Mul)]
    [InlineData(Operator.Div)]
    [InlineData(Operator.Pow)]
    [InlineData(Operator.Mod)]
    public void Parse_ChainOneOverTheDepthLimit_IsTooDeep(Operator op)
    {
        var ex = Assert.Throws<CalcException>(() => Parser.Parse(LongChain(TestConstants.MaxDepthLimit + 1, _ => op)));

        Assert.Equal(CalcErrorKind.ExpressionTooDeep, ex.Kind);
    }

    [Theory]
    [InlineData(Operator.Add)]
    [InlineData(Operator.Pow)]
    public void Parse_MillionOperatorChain_IsRejectedWithoutOverflowingTheStack(Operator op)
    {
        var ex = Assert.Throws<CalcException>(() => Parser.Parse(LongChain(1_000_000, _ => op)));

        Assert.Equal(CalcErrorKind.ExpressionTooDeep, ex.Kind);
    }

    [Fact]
    public void Parse_ParenthesesNestedUpToTheLimit_Work()
    {
        var expr = Parser.Parse(NestedParentheses(TestConstants.MaxDepthLimit));

        Assert.Equal(N(0), expr);
    }

    [Fact]
    public void Parse_ParenthesesNestedBeyondTheLimit_AreTooDeep()
    {
        var ex = Assert.Throws<CalcException>(() => Parser.Parse(NestedParentheses(TestConstants.MaxDepthLimit + 1)));

        Assert.Equal(CalcErrorKind.ExpressionTooDeep, ex.Kind);
    }

    [Fact]
    public void Parse_HugeParenthesisNesting_IsRejectedWithoutOverflowingTheStack()
    {
        var ex = Assert.Throws<CalcException>(() => Parser.Parse(NestedParentheses(200_000)));

        Assert.Equal(CalcErrorKind.ExpressionTooDeep, ex.Kind);
    }

    [Fact]
    public void Parse_HugeRunOfOpeningParentheses_IsTooDeepNotUnbalanced()
    {
        var tokens = Enumerable.Repeat<Token>(LParen(), 200_000).ToList();

        var ex = Assert.Throws<CalcException>(() => Parser.Parse(tokens));

        Assert.Equal(CalcErrorKind.ExpressionTooDeep, ex.Kind);
    }

    [Fact]
    public void Parse_LongChainOfShallowGroups_IsTooDeepBecauseTheChainIsTall()
    {
        // (1+1)*(1+1)*...: each group is shallow, but 1999 chained operators make the tree 1999 levels tall
        var tokens = new List<Token>();
        for (var i = 0; i < 2000; i++)
        {
            if (i > 0)
            {
                tokens.Add(Op(Operator.Mul));
            }

            tokens.AddRange([LParen(), Num(1), Op(Operator.Add), Num(1), RParen()]);
        }

        var ex = Assert.Throws<CalcException>(() => Parser.Parse(tokens));

        Assert.Equal(CalcErrorKind.ExpressionTooDeep, ex.Kind);
    }

    [Fact]
    public void Parse_ManyShallowGroupsInsideTheLimit_Work()
    {
        // 500 groups chained with one operator: tree height 500 + 1
        var tokens = new List<Token>();
        for (var i = 0; i < 500; i++)
        {
            if (i > 0)
            {
                tokens.Add(Op(Operator.Mul));
            }

            tokens.AddRange([LParen(), Num(1), Op(Operator.Add), Num(1), RParen()]);
        }

        var expr = Parser.Parse(tokens);

        Assert.Equal(1000, CountNumbers(expr));
    }

    private static UnaryExpr Neg(Expr operand) => new(UnaryOperator.Neg, operand);

    private static List<Token> Minuses(int count, double num = 0)
    {
        var tokens = new List<Token>();
        tokens.AddRange(Enumerable.Repeat<Token>(Op(Operator.Sub), count));
        tokens.Add(Num(num));
        return tokens;
    }

    [Fact]
    public void Parse_UnaryMinusOnANumber()
    {
        // -5
        var expr = Parser.Parse([Op(Operator.Sub), Num(5)]);

        Assert.Equal(Neg(N(5)), expr);
    }

    [Fact]
    public void Parse_UnaryMinusAfterAnOperator()
    {
        // 2*-3
        var expr = Parser.Parse([Num(2), Op(Operator.Mul), Op(Operator.Sub), Num(3)]);

        Assert.Equal(B(Operator.Mul, N(2), Neg(N(3))), expr);
    }

    [Fact]
    public void Parse_UnaryMinusOnAParenthesisedGroup()
    {
        // -(2+3)
        var expr = Parser.Parse([Op(Operator.Sub), LParen(), Num(2), Op(Operator.Add), Num(3), RParen()]);

        Assert.Equal(Neg(B(Operator.Add, N(2), N(3))), expr);
    }

    [Fact]
    public void Parse_UnaryMinusInsideParentheses()
    {
        // (-2)
        var expr = Parser.Parse([LParen(), Op(Operator.Sub), Num(2), RParen()]);

        Assert.Equal(Neg(N(2)), expr);
    }

    [Fact]
    public void Parse_DoubleUnaryMinus()
    {
        // --5
        var expr = Parser.Parse([Op(Operator.Sub), Op(Operator.Sub), Num(5)]);

        Assert.Equal(Neg(Neg(N(5))), expr);
    }

    [Fact]
    public void Parse_TripleUnaryMinus()
    {
        var expr = Parser.Parse(Minuses(3, 5));

        Assert.Equal(Neg(Neg(Neg(N(5)))), expr);
    }

    [Fact]
    public void Parse_UnaryMinusBindsLooserThanPower()
    {
        // -2^2 is -(2^2)
        var expr = Parser.Parse([Op(Operator.Sub), Num(2), Op(Operator.Pow), Num(2)]);

        Assert.Equal(Neg(B(Operator.Pow, N(2), N(2))), expr);
    }

    [Fact]
    public void Parse_ParenthesesMakeTheNegationBindTighterThanPower()
    {
        // (-2)^2
        var expr = Parser.Parse(
            [LParen(), Op(Operator.Sub), Num(2), RParen(), Op(Operator.Pow), Num(2)]);

        Assert.Equal(B(Operator.Pow, Neg(N(2)), N(2)), expr);
    }

    [Fact]
    public void Parse_UnaryMinusInTheExponent()
    {
        // 2^-2
        var expr = Parser.Parse([Num(2), Op(Operator.Pow), Op(Operator.Sub), Num(2)]);

        Assert.Equal(B(Operator.Pow, N(2), Neg(N(2))), expr);
    }

    [Fact]
    public void Parse_UnaryMinusOnAPowerWithANegativeExponent()
    {
        // -2^-2 is -(2^(-2))
        var expr = Parser.Parse(
            [Op(Operator.Sub), Num(2), Op(Operator.Pow), Op(Operator.Sub), Num(2)]);

        Assert.Equal(Neg(B(Operator.Pow, N(2), Neg(N(2)))), expr);
    }

    [Fact]
    public void Parse_UnaryMinusInTheExponentTakesTheWholePowerTower()
    {
        // 2^-3^2 is 2^(-(3^2))
        var expr = Parser.Parse(
            [Num(2), Op(Operator.Pow), Op(Operator.Sub), Num(3), Op(Operator.Pow), Num(2)]);

        Assert.Equal(B(Operator.Pow, N(2), Neg(B(Operator.Pow, N(3), N(2)))), expr);
    }

    [Theory]
    [InlineData(Operator.Mul)]
    [InlineData(Operator.Div)]
    [InlineData(Operator.Add)]
    [InlineData(Operator.Sub)]
    [InlineData(Operator.Mod)]
    public void Parse_UnaryMinusBindsTighterThanTheOtherBinaryOperators(Operator op)
    {
        // -2*3, -2/3, -2+3, -2-3: only the 2 is negated
        var expr = Parser.Parse([Op(Operator.Sub), Num(2), Op(op), Num(3)]);

        Assert.Equal(B(op, Neg(N(2)), N(3)), expr);
    }

    [Fact]
    public void Parse_BinaryMinusFollowedByUnaryMinus()
    {
        // 2--3
        var expr = Parser.Parse([Num(2), Op(Operator.Sub), Op(Operator.Sub), Num(3)]);

        Assert.Equal(B(Operator.Sub, N(2), Neg(N(3))), expr);
    }

    [Fact]
    public void Parse_BinaryMinusFollowedByTwoUnaryMinuses()
    {
        // 2---3
        var expr = Parser.Parse(
            [Num(2), Op(Operator.Sub), Op(Operator.Sub), Op(Operator.Sub), Num(3)]);

        Assert.Equal(B(Operator.Sub, N(2), Neg(Neg(N(3)))), expr);
    }

    [Fact]
    public void Parse_UnaryMinusOnBothOperands()
    {
        // -2*-3
        var expr = Parser.Parse(
            [Op(Operator.Sub), Num(2), Op(Operator.Mul), Op(Operator.Sub), Num(3)]);

        Assert.Equal(B(Operator.Mul, Neg(N(2)), Neg(N(3))), expr);
    }

    [Fact]
    public void Parse_UnaryMinusOnAGroupThenAnOperator()
    {
        // -(2+3)*4
        var expr = Parser.Parse(
            [Op(Operator.Sub), LParen(), Num(2), Op(Operator.Add), Num(3), RParen(), Op(Operator.Mul), Num(4)]);

        Assert.Equal(B(Operator.Mul, Neg(B(Operator.Add, N(2), N(3))), N(4)), expr);
    }

    [Fact]
    public void Parse_NegatedGroupInsideANegatedGroup()
    {
        // -(-2)
        var expr = Parser.Parse(
            [Op(Operator.Sub), LParen(), Op(Operator.Sub), Num(2), RParen()]);

        Assert.Equal(Neg(Neg(N(2))), expr);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void Parse_LoneMinus_Throws(int count)
    {
        // - and --
        var tokens = Enumerable.Repeat<Token>(Op(Operator.Sub), count).ToList();

        var ex = Assert.Throws<CalcException>(() => Parser.Parse(tokens));

        Assert.Equal(CalcErrorKind.UnexpectedEnd, ex.Kind);
    }

    [Fact]
    public void Parse_UnaryMinusBeforeAnOperator_Throws()
    {
        // -*2
        var ex = Assert.Throws<CalcException>(
            () => Parser.Parse([Op(Operator.Sub), Op(Operator.Mul), Num(2)]));

        Assert.Equal(CalcErrorKind.UnexpectedToken, ex.Kind);
    }

    [Fact]
    public void Parse_OperatorThenMinusAtTheEnd_Throws()
    {
        // 2*-
        var ex = Assert.Throws<CalcException>(
            () => Parser.Parse([Num(2), Op(Operator.Mul), Op(Operator.Sub)]));

        Assert.Equal(CalcErrorKind.UnexpectedEnd, ex.Kind);
    }

    [Fact]
    public void Parse_UnaryMinusBeforeAClosingParenthesis_Throws()
    {
        // (-)
        var ex = Assert.Throws<CalcException>(
            () => Parser.Parse([LParen(), Op(Operator.Sub), RParen()]));

        Assert.Equal(CalcErrorKind.UnexpectedToken, ex.Kind);
    }

    [Fact]
    public void Parse_UnaryMinusBeforeAnOpeningParenthesisAtTheEnd_Throws()
    {
        // -(
        var ex = Assert.Throws<CalcException>(() => Parser.Parse([Op(Operator.Sub), LParen()]));

        Assert.Equal(CalcErrorKind.UnexpectedEnd, ex.Kind);
    }

    [Fact]
    public void Parse_UnaryMinusOnAGroupMissingItsClosingParenthesis_Throws()
    {
        // -(2
        var ex = Assert.Throws<CalcException>(
            () => Parser.Parse([Op(Operator.Sub), LParen(), Num(2)]));

        Assert.Equal(CalcErrorKind.UnbalancedParentheses, ex.Kind);
    }

    [Theory]
    [InlineData(Operator.Add)]
    [InlineData(Operator.Mul)]
    [InlineData(Operator.Div)]
    [InlineData(Operator.Pow)]
    [InlineData(Operator.Mod)]
    public void Parse_OtherOperatorsAreNotUnary(Operator op)
    {
        // +5, *5, /5, ^5
        var ex = Assert.Throws<CalcException>(() => Parser.Parse([Op(op), Num(5)]));

        Assert.Equal(CalcErrorKind.UnexpectedToken, ex.Kind);
    }

    [Fact]
    public void Parse_UnaryPlusAfterAnOperator_Throws()
    {
        // 2*+3
        var ex = Assert.Throws<CalcException>(
            () => Parser.Parse([Num(2), Op(Operator.Mul), Op(Operator.Add), Num(3)]));

        Assert.Equal(CalcErrorKind.UnexpectedToken, ex.Kind);
    }

    [Fact]
    public void Parse_NegationsAtTheDepthLimit_Work()
    {
        var expr = Parser.Parse(Minuses(TestConstants.MaxDepthLimit, 5));

        // the innermost operand is reached after exactly TestConstants.MaxDepthLimit unary levels
        var depth = 0;
        while (expr is UnaryExpr unary)
        {
            depth++;
            expr = unary.Operand;
        }

        Assert.Equal(TestConstants.MaxDepthLimit, depth);
        Assert.Equal(N(5), expr);
    }

    [Fact]
    public void Parse_NegationsOneOverTheDepthLimit_AreTooDeep()
    {
        var ex = Assert.Throws<CalcException>(() => Parser.Parse(Minuses(TestConstants.MaxDepthLimit + 1)));

        Assert.Equal(CalcErrorKind.ExpressionTooDeep, ex.Kind);
    }

    [Fact]
    public void Parse_HugeRunOfNegations_IsRejectedWithoutOverflowingTheStack()
    {
        var ex = Assert.Throws<CalcException>(() => Parser.Parse(Minuses(1_000_000)));

        Assert.Equal(CalcErrorKind.ExpressionTooDeep, ex.Kind);
    }

    [Fact]
    public void Parse_ChainOfNegatedTermsInsideTheLimit_Works()
    {
        // 1 + -1 + -1 ... with 600 negated terms: the chain is 600 levels tall, within the limit
        var tokens = new List<Token> { Num(1) };
        for (var i = 0; i < 600; i++)
        {
            tokens.Add(Op(Operator.Add));
            tokens.Add(Op(Operator.Sub));
            tokens.Add(Num(1));
        }

        var expr = Parser.Parse(tokens);

        Assert.Equal(601, CountNumbers(expr));
    }

    [Fact]
    public void Parse_TallChainOfNegatedTerms_IsTooDeep()
    {
        // 1 + -1 + -1 ... with 1100 terms: each Add level is one level taller, so the chain exceeds the limit
        var tokens = new List<Token> { Num(1) };
        for (var i = 0; i < 1100; i++)
        {
            tokens.Add(Op(Operator.Add));
            tokens.Add(Op(Operator.Sub));
            tokens.Add(Num(1));
        }

        var ex = Assert.Throws<CalcException>(() => Parser.Parse(tokens));

        Assert.Equal(CalcErrorKind.ExpressionTooDeep, ex.Kind);
    }

    [Fact]
    public void Parse_CountNumbers_IncludesNumbersUnderNegations()
    {
        // the helper must walk through UnaryExpr nodes: -(1+2) has two numbers
        var expr = Parser.Parse([Op(Operator.Sub), LParen(), Num(1), Op(Operator.Add), Num(2), RParen()]);

        Assert.Equal(2, CountNumbers(expr));
    }

    private static List<Token> NestedParentheses(int depth, double num = 0)
    {
        var tokens = new List<Token>();
        tokens.AddRange(Enumerable.Repeat<Token>(LParen(), depth));
        tokens.Add(Num(num));
        tokens.AddRange(Enumerable.Repeat<Token>(RParen(), depth));
        return tokens;
    }

    private static List<Token> LongChain(int operatorCount, Func<int, Operator> operatorAt)
    {
        var tokens = new List<Token> { Num(1) };
        for (var i = 0; i < operatorCount; i++)
        {
            tokens.Add(Op(operatorAt(i)));
            tokens.Add(Num(1));
        }

        return tokens;
    }

    // Iterative on purpose: a recursive helper would itself overflow on deep trees.
    private static int CountNumbers(Expr root)
    {
        var count = 0;
        var pending = new Stack<Expr>();
        pending.Push(root);

        while (pending.Count > 0)
        {
            switch (pending.Pop())
            {
                case NumberExpr or ConstantExpr:
                    count++;
                    break;
                case BinaryExpr binary:
                    pending.Push(binary.Left);
                    pending.Push(binary.Right);
                    break;
                case UnaryExpr unary:
                    pending.Push(unary.Operand);
                    break;
                case CallExpr call:
                    pending.Push(call.Argument);
                    break;
            }
        }

        return count;
    }
}
