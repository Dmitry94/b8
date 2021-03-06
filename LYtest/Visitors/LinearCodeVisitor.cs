﻿using LYtest.LinearRepr;
using LYtest.LinearRepr.Values;
using ProgramTree;
using System.Collections.Generic;
using System.Linq;

namespace LYtest.Visitors
{
    class LinearCodeVisitor : IVisitor
    {

        private static readonly string CONSTANT_PREFIX = "$const";
        private static readonly string LABEL_PREFIX = "%label";

        private static readonly Dictionary<Operator, Operation> OPERATOR_TO_OPERATION =
            new Dictionary<Operator, Operation>
            {
                {Operator.Plus, Operation.Plus },
                {Operator.Minus, Operation.Minus },
                {Operator.Mult, Operation.Mult }
                // todo: дополнить остальными значениями
            };

        public List<LinearRepresentation> code = new List<LinearRepresentation>();
        private List<LinearRepresentation> evaluatedExpression = new List<LinearRepresentation>();

        private int valueCounter = 0;
        private int labelCounter = 0;

        private IValue idOrNum;

        /**
         * Описания методов визитора
         */

        public void VisitIf(IfNode n)
        {
            branchCondition(n.Cond, n.TrueChild, n.FalseChild);
        }

        public void VisitFor(ForNode n)
        {
            // todo 22.03 ?
        }

        public void VisitAssign(AssignNode n)
        {
            LinearRepresentation resAssign;
            n.Expr.AcceptVisit(this);
            if (evaluatedExpression.Any())
            {
                resAssign = evaluatedExpression.Last();
                evaluatedExpression.RemoveAt(evaluatedExpression.Count - 1);
                --valueCounter;
            }
            else
            {
                resAssign = new LinearRepresentation(Operation.Assign, null, idOrNum);
            }
            n.Id.AcceptVisit(this);
            resAssign.Destination = (IdentificatorValue)idOrNum;
            evaluatedExpression.Add(resAssign);

            moveExpressionToCode();
        }

        public void VisitBlock(BlockNode n)
        {
            n.Childs.ToList().ForEach(c => c.AcceptVisit(this));
        }

        public void VisitProc(Procedure n)
        {
            // todo 22.03 ?
        }

        public void VisitWhile(WhileNode n)
        {
            // todo 22.03 ?
        }

        public void VisitConst(Const n)
        {
            idOrNum = new NumericValue(n.Val);
        }

        public void VisitId(IdentNode n)
        {
            idOrNum = new IdentificatorValue(n.Name);
        }

        public void VisitBinOp(BinOp n)
        {
            var result = new LinearRepresentation(operatorToOperation(n.Op));
            n.Lhs.AcceptVisit(this);
            result.LeftOperand = idOrNum;
            n.Rhs.AcceptVisit(this);
            result.RightOperand = idOrNum;

            var identificator = new IdentificatorValue(CONSTANT_PREFIX + valueCounter++.ToString());
            idOrNum = identificator;
            result.Destination = identificator;
            evaluatedExpression.Add(result);
        }

        public void VisitUnOp(UnOp n)
        {
            // Todo ?
        }

        /**
          * Вспомогательные методы
          */

        private Operation operatorToOperation(Operator op)
        {
            return OPERATOR_TO_OPERATION[op];
        }

        private void moveExpressionToCode()
        {
            code.AddRange(evaluatedExpression);
            evaluatedExpression.Clear();
        }

        private void branchCondition(ExprNode cond, 
                                     StatementNode trueBranch, 
                                     StatementNode falseBranch,
                                     List<LinearRepresentation> addBeforeEndLabel = null)
        {
            cond.AcceptVisit(this);
            LabelValue trueCond = new LabelValue(LABEL_PREFIX + labelCounter++);
            LabelValue endCond = new LabelValue(LABEL_PREFIX + labelCounter++);

            LinearRepresentation gotoCond = new LinearRepresentation(Operation.CondGoto, trueCond, idOrNum);
            evaluatedExpression.Add(gotoCond);
            moveExpressionToCode();

            if (falseBranch != null)
                falseBranch.AcceptVisit(this);
            
            evaluatedExpression.Add(new LinearRepresentation(Operation.Goto, endCond));
            evaluatedExpression.Add(new LinearRepresentation(trueCond, Operation.NoOperation));
            moveExpressionToCode();

            trueBranch.AcceptVisit(this);
            if (addBeforeEndLabel != null)
                evaluatedExpression.AddRange(addBeforeEndLabel);
            
            evaluatedExpression.Add(new LinearRepresentation(endCond, Operation.NoOperation));
            moveExpressionToCode();
        }
    }
}
