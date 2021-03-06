﻿namespace MethodCache.Tests.TestAssembly
{
	using MethodCache.Attributes;
	using MethodCache.Tests.TestAssembly.Cache;

	public class TestClassIndividualProperties
	{
		private string field;

		private int someValue;

		public TestClassIndividualProperties(ICacheWithRemove cache)
		{
			Cache = cache;
		}

		[Cache]
		public int AutoProperty { get; set; }

		public ICacheWithRemove Cache { get; private set; }

		[Cache]
		public string ReadOnlyProperty
		{
			get { return "some value"; }
		}

		[Cache]

		// ReSharper disable once ConvertToAutoProperty
		public int ReadWriteProperty
		{
			get { return this.someValue; }
			set { this.someValue = value; }
		}

		[Cache]
		public string SetOnlyProperty
		{
			set { this.field = value; }
		}
	}
}