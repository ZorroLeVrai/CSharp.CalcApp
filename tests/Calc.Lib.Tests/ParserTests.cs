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
    public void Parse_LongChainOf1024Operators_DoesNotOverflow(Operator op)
    {
        var tokens = LongChain(1024, _ => op);

        var expr = Parser.Parse(tokens);

        Assert.Equal(1025, CountNumbers(expr));
    }

    [Fact]
    public void Parse_LongChainOf1024MixedOperators_DoesNotOverflow()
    {
        Operator[] cycle = [Operator.Add, Operator.Sub, Operator.Mul, Operator.Div, Operator.Pow, Operator.Mod];
        var tokens = LongChain(1024, i => cycle[i % cycle.Length]);

        var expr = Parser.Parse(tokens);

        Assert.Equal(1025, CountNumbers(expr));
    }

    [Fact]
    public void Parse_LongChainViaTokenizer_DoesNotOverflow()
    {
        var input = string.Join("+", Enumerable.Repeat("1", 1025));

        var expr = Parser.Parse(Tokenizer.Tokenize(input));

        Assert.Equal(1025, CountNumbers(expr));
    }

    [Theory]
    [InlineData(Operator.Add)]
    [InlineData(Operator.Sub)]
    [InlineData(Operator.Mul)]
    [InlineData(Operator.Div)]
    [InlineData(Operator.Pow)]
    [InlineData(Operator.Mod)]
    public void Parse_ChainOf1025Operators_IsTooDeep(Operator op)
    {
        var ex = Assert.Throws<CalcException>(() => Parser.Parse(LongChain(1025, _ => op)));

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
        var expr = Parser.Parse(NestedParentheses(1024));

        Assert.Equal(N(0), expr);
    }

    [Fact]
    public void Parse_ParenthesesNestedBeyondTheLimit_AreTooDeep()
    {
        var ex = Assert.Throws<CalcException>(() => Parser.Parse(NestedParentheses(1025)));

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
    public void Parse_ThousandAndTwentyFourNegations_Work()
    {
        var expr = Parser.Parse(Minuses(1024, 5));

        // the innermost operand is reached after exactly 1024 unary levels
        var depth = 0;
        while (expr is UnaryExpr unary)
        {
            depth++;
            expr = unary.Operand;
        }

        Assert.Equal(1024, depth);
        Assert.Equal(N(5), expr);
    }

    [Fact]
    public void Parse_ThousandAndTwentyFiveNegations_AreTooDeep()
    {
        var ex = Assert.Throws<CalcException>(() => Parser.Parse(Minuses(1025)));

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
                case NumberExpr:
                    count++;
                    break;
                case BinaryExpr binary:
                    pending.Push(binary.Left);
                    pending.Push(binary.Right);
                    break;
                case UnaryExpr unary:
                    pending.Push(unary.Operand);
                    break;
            }
        }

        return count;
    }
}
