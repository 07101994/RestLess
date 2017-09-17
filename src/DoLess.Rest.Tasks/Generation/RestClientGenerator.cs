﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using DoLess.Rest.Tasks.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace DoLess.Rest.Tasks
{
    internal class RestClientGenerator : CSharpSyntaxRewriter
    {
        private RequestInfo requestInfo;
        private RequestInfo methodRequestInfo;
        private string className;

        private RestClientGenerator()
        {
        }

        public static SyntaxNode Generate(SyntaxNode rootNode)
        {
            var generator = new RestClientGenerator();
            return generator.Visit(rootNode);
        }

        public static ClassDeclarationSyntax Generate(InterfaceDeclarationSyntax interfaceDeclarationSyntax)
        {
            var generator = new RestClientGenerator();
            return generator.Visit(interfaceDeclarationSyntax) as ClassDeclarationSyntax;
        }

        public override SyntaxNode VisitInterfaceDeclaration(InterfaceDeclarationSyntax node)
        {
            if (node.IsRestInterface())
            {
                this.requestInfo = new RequestInfo(node);

                this.className = $"{Constants.RestClientPrefix}{node.Identifier.ValueText}";
                var classDeclaration = ClassDeclaration(className)
                                      .AddModifiers(Token(SyntaxKind.InternalKeyword))
                                      .AddModifiers(Token(SyntaxKind.SealedKeyword))
                                      .WithTypeParameterList(node.TypeParameterList)
                                      .WithConstraintClauses(node.ConstraintClauses)
                                      .AddMembers(ImplementConstructor())
                                      .AddMembers(ImplementMethods(node.Members))
                                      .WithBaseList(BaseList(SeparatedList<BaseTypeSyntax>(new[]
                                      {
                                          SimpleBaseType(IdentifierName(nameof(RestClient))),
                                          SimpleBaseType(node.GetTypeSyntax())
                                      })));

                return classDeclaration;
            }
            else
            {
                return null;
            }
        }

        public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            return null;
        }

        public override SyntaxNode VisitDelegateDeclaration(DelegateDeclarationSyntax node)
        {
            return null;
        }

        public override SyntaxNode VisitEnumDeclaration(EnumDeclarationSyntax node)
        {
            return null;
        }

        public override SyntaxNode VisitStructDeclaration(StructDeclarationSyntax node)
        {
            return null;
        }

        private MemberDeclarationSyntax ImplementConstructor()
        {
            return ConstructorDeclaration(this.className)
                  .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword)))
                  .WithParameterList(NewParameterList(NewParameter(nameof(HttpClient), "httpClient"), NewParameter(nameof(RestSettings), "settings")))
                  .WithInitializer(ConstructorInitializer(SyntaxKind.BaseConstructorInitializer, NewArgumentList("httpClient", "settings")))
                  .WithBody(Block());
        }

        private MemberDeclarationSyntax[] ImplementMethods(SyntaxList<MemberDeclarationSyntax> syntaxList)
        {
            return syntaxList.OfType<MethodDeclarationSyntax>()
                             .Select(x => this.ImplementMethod(x))
                             .ToArray();
        }

        private MethodDeclarationSyntax ImplementMethod(MethodDeclarationSyntax node)
        {
            var methodDeclaration = node.WithoutAttributes()
                                        .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword)))
                                        .WithTypeParameterList(node.TypeParameterList.WithoutAttributes())
                                        .WithParameterList(node.ParameterList.WithoutAttributes())
                                        .WithBody(this.ImplementMethodBody(node))
                                        .WithSemicolonToken(MissingToken(SyntaxKind.SemicolonToken));

            return methodDeclaration;
        }



        private BlockSyntax ImplementMethodBody(MethodDeclarationSyntax node)
        {
            this.methodRequestInfo = this.requestInfo.WithMethod(node);

            return Block(ReturnStatement(ImplementRequest()));
        }

        private ExpressionSyntax ImplementRequest()
        {
            InvocationExpressionSyntax result = NewMethodInvocation(nameof(RestRequest), this.methodRequestInfo.HttpMethod)
                                               .WithArgs(Argument(ThisExpression()));

            result = this.ChainWithRequestUrlBuilding(result);
            result = this.ChainWithHeaders(result);
            result = this.ChainWithBody(result);
            result = this.ChainWithSendMethod(result);

            return result;
        }

        private InvocationExpressionSyntax ChainWithRequestUrlBuilding(InvocationExpressionSyntax invocationExpression)
        {
            //Add the BaseUrl if any.
            if (!string.IsNullOrEmpty(this.methodRequestInfo.BaseUrl))
            {
                invocationExpression = invocationExpression.ChainWith(nameof(RestRequest.WithBaseUrl))
                                                           .WithArgs(this.methodRequestInfo.BaseUrl.ToArgLiteral());
            }
            invocationExpression = invocationExpression.ChainWith(nameof(RestRequest.WithUriTemplate))
                                                       .WithArgs(this.methodRequestInfo.UriTemplate.ToArgLiteral());

            invocationExpression = this.ChainWithUriParameters(invocationExpression);

            return invocationExpression;
        }

        private InvocationExpressionSyntax ChainWithParameters(InvocationExpressionSyntax invocationExpression, IReadOnlyDictionary<string, Parameter> parameters, string methodName)
        {
            var headers = this.methodRequestInfo.Headers;
            if (parameters.Count > 0)
            {
                parameters.ForEach(x =>
                {
                    invocationExpression = invocationExpression.ChainWith(methodName)
                                                               .WithArgs(x.Key.ToArgLiteral(), x.Value.ToArg());
                });
            }

            return invocationExpression;
        }

        private InvocationExpressionSyntax ChainWithHeaders(InvocationExpressionSyntax invocationExpression)
        {
            return this.ChainWithParameters(invocationExpression, this.methodRequestInfo.Headers, nameof(RestRequest.WithHeader));
        }

        private InvocationExpressionSyntax ChainWithUriParameters(InvocationExpressionSyntax invocationExpression)
        {
            return this.ChainWithParameters(invocationExpression, this.methodRequestInfo.UriVariables, nameof(RestRequest.WithParameter));
        }

        private InvocationExpressionSyntax ChainWithBody(InvocationExpressionSyntax invocationExpression)
        {
            var bodyIdentifier = this.methodRequestInfo.BodyIdentifier;
            if (bodyIdentifier.HasContent())
            {
                string methodName = this.methodRequestInfo.IsBodyFormUrlEncoded ?
                                    nameof(RestRequest.WithFormUrlEncodedBody) :
                                    nameof(RestRequest.WithBody);

                invocationExpression = invocationExpression.ChainWith(methodName)
                                                           .WithArgs(bodyIdentifier.ToArg());
            }

            return invocationExpression;
        }

        private InvocationExpressionSyntax ChainWithSendMethod(InvocationExpressionSyntax invocationExpression)
        {
            TypeSyntax returnType = this.methodRequestInfo.MethodDeclaration.ReturnType;
            TypeSyntax genericType = (returnType as GenericNameSyntax)?.TypeArgumentList?.Arguments.FirstOrDefault();

            invocationExpression = this.ChainWithSendMethod(invocationExpression, genericType);

            string cancellationTokenParameterName = this.methodRequestInfo
                                                        .MethodDeclaration
                                                        .ParameterList?
                                                        .Parameters
                                                        .FirstOrDefault(x => x.Type.GetTypeName() == nameof(CancellationToken))?
                                                        .Identifier
                                                        .ValueText;

            if (cancellationTokenParameterName.HasContent())
            {
                return invocationExpression.WithArgs(cancellationTokenParameterName.ToArg());
            }

            return invocationExpression;
        }

        private InvocationExpressionSyntax ChainWithSendMethod(InvocationExpressionSyntax invocationExpression, TypeSyntax returnType)
        {
            switch (returnType)
            {
                case null:
                    // Task.
                    return invocationExpression.ChainWith(nameof(RestRequest.SendAsync));

                case ArrayTypeSyntax type01 when type01.ElementType.GetTypeName() == nameof(Byte):
                case ArrayTypeSyntax type02 when type02.ElementType is PredefinedTypeSyntax elementType &&
                                                 elementType.Keyword.IsKind(SyntaxKind.ByteKeyword):
                    // Task<byte[]>.
                    return invocationExpression.ChainWith(nameof(RestRequest.ReadAsByteArrayAsync));

                case PredefinedTypeSyntax predefinedType when predefinedType.Keyword.IsKind(SyntaxKind.StringKeyword):
                case var simpleType when simpleType.GetTypeName() == nameof(String):
                    // Task<string>.
                    return invocationExpression.ChainWith(nameof(RestRequest.ReadAsStringAsync));

                case var type when type.GetTypeName() == nameof(Stream):
                    // Task<Stream>.
                    return invocationExpression.ChainWith(nameof(RestRequest.ReadAsStreamAsync));

                case PredefinedTypeSyntax predefinedType when predefinedType.Keyword.IsKind(SyntaxKind.BoolKeyword):
                case var simpleType when simpleType.GetTypeName() == nameof(Boolean):
                    // Task<bool>.
                    return invocationExpression.ChainWith(nameof(RestRequest.SendAndGetSuccessAsync));

                case var simpleType when simpleType.GetTypeName() == nameof(HttpResponseMessage):
                    // Task<HttpResponseMessage>.
                    return invocationExpression.ChainWith(nameof(RestRequest.ReadAsHttpResponseMessageAsync));

                default:
                    // Task<T>.
                    return invocationExpression.ChainWith(nameof(RestRequest.ReadAsObject), returnType);
            }
        }

        private static ParameterSyntax NewParameter(string type, string identifier)
        {
            return Parameter(Identifier(identifier)).WithType(IdentifierName(type));
        }

        private static ParameterListSyntax NewParameterList(params ParameterSyntax[] parameters)
        {
            return ParameterList(SeparatedList(parameters));
        }

        private static ArgumentListSyntax NewArgumentList(params string[] identifiers)
        {
            return ArgumentList(SeparatedList(identifiers.Select(x => x.ToArg())));
        }

        private static InvocationExpressionSyntax NewMethodInvocation(string identifier, string method)
        {
            return
                InvocationExpression(
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        IdentifierName(identifier),
                        IdentifierName(method)));
        }

        private static ArgumentSyntax NewFalseArgument()
        {
            return Argument(LiteralExpression(SyntaxKind.FalseLiteralExpression));
        }

        private static ArgumentSyntax NewTrueArgument()
        {
            return Argument(LiteralExpression(SyntaxKind.TrueLiteralExpression));
        }

    }
}
