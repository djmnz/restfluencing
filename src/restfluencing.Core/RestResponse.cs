﻿using System;
using System.Collections.Generic;
using System.Linq;
using restfluencing.Assertion;
using restfluencing.Assertion.Rules;
using restfluencing.Client;
using restfluencing.Helpers;

namespace restfluencing
{
	public class RestResponse
	{
		private const string ERROR_NO_ASSERTION =
			"There is no Assertion configured. Use Rest.Configuration.AssertWithException() for the default assertion.";

		protected RestRequest Request { get; private set; }
		protected AssertionContext Context { get; }
		public bool DelayAssertion { get; }


		/// <summary>
		/// Constructor used for sequential execution of the tests (specflow style)
		/// </summary>
		/// <param name="request"></param>
		/// <param name="context"></param>
		public RestResponse(RestRequest request, AssertionContext context, bool delayAssertion) 
		{
			Request = request ?? throw new ArgumentNullException(nameof(request), "No RestRequest has been provided.");
			Context = context ?? throw new ArgumentException(nameof(context), "No AssertionContext has been provided.");
			DelayAssertion = delayAssertion;
		}

		
		private IList<AssertionRule> _rules = new List<AssertionRule>();

		/// <summary>
		/// List of rules that are attached to this response.
		/// </summary>
		public IEnumerable<AssertionRule> Rules => _rules;

		/// <summary>
		/// Adds a rule and if DelayAssertion is false will assert the rule as well.
		/// </summary>
		/// <param name="rule"></param>
		public void AddRule(AssertionRule rule)
		{
			_rules.Add(rule);

			if (!DelayAssertion)
			{
				if (Request.Assertion == null)
				{
					throw new InvalidOperationException(ErrorMessages.NoAssertion);
				}

				var result = CreateExecutionResult();
				result.Results.AddRange(rule.Assert(Context));
				Request.Assertion.Assert(result);
			}

		}

		/// <summary>
		/// Removes all previous rules of same type and adds the new rule.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="rule"></param>
		public void OnlyOneRuleOf<T>(T rule) where T : AssertionRule
		{
			Array.ForEach(_rules.Where(r => r is T).ToArray(), r => _rules.Remove(r));
			AddRule(rule);
		}

		/// <summary>
		/// Gives the results of the assertion rules against the assertion context (response).
		/// </summary>
		/// <returns></returns>
		/// <remarks>
		/// This is essential when using the DelayAssertion = true
		/// </remarks>
		public ExecutionResult Execute()
		{
			var result = CreateExecutionResult();

			foreach (var rule in Rules)
			{
				result.Results.AddRange(rule.Assert(Context));
			}

			return result;
		}


		/// <summary>
		/// Assert all the results of the assertion rules against the assertion context (response).
		/// </summary>
		/// <remarks>
		/// This is essential when using the DelayAssertion = true
		/// </remarks>
		public void Assert()
		{
			if (Request.Assertion == null)
			{
				throw new InvalidOperationException(ErrorMessages.NoAssertion);
			}
			Request.Assertion.Assert(Execute());
		}

		private ExecutionResult CreateExecutionResult()
		{
			var result = new ExecutionResult();
			result.Response = Context.Response;

			var assertionResults = new List<AssertionResult>();
			result.Results = assertionResults;

			return result;
		}
	}
}