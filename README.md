## This is an add-in for [Fody](https://github.com/Fody/Fody/) 

Caches return values of methods and properties decorated with a `[Cache]` Attribute.

[Introduction to Fody](http://github.com/Fody/Fody/wiki/SampleUsage).

# Nuget package

There is a nuget package avaliable here http://nuget.org/packages/MethodCache.Fody.

## Your Code

	[Cache]
	public int Add(int a, int b)
	{
		return a + b;
	}

	[Cache]
	public string AlsoWorksForProperties
	{
		get
		{
			return DoSomeCalculations(this.parameterField);
		}
		set
		{
			this.parameterField = value;
		}
	}

## What gets compiled

	public int Add(int a, int b)
	{
		string cacheKey = string.Format("Namespace.Class.Add_{0}_{1}", new object[] { a, b });
	
		if(Cache.Contains(cacheKey))
		{
			return Cache.Retrieve<int>(cacheKey);
		}
		
		int result = a + b;
		
		Cache.Store(cacheKey, result);
		
		return result;
	}

	public object AlsoWorksForProperties
	{
		get
		{
			string cacheKey = "Namespace.Class.AlsoWorksForProperties";
		
			if(Cache.Contains(cacheKey))
			{
				return Cache.Retrieve<int>(cacheKey);
			}
		
			object result = DoSomeCalculations(this.parameterField);
			
			Cache.Store(cacheKey, result);
			
			return result;
		}
		set
		{
			string cacheKey = "Namespace.Class.AlsoWorksForProperties";
			Cache.Remove(cacheKey);
			this.parameterField = value;
		}
	}

# How to use

  * Install MethodCache.Fody via Nuget
  * Create an Cache Implementation (MemCache, FileCache, DBCache, ...) which implements Contains, Retrieve, Store and Remove methods.

Optional

  * Add your own CacheAttribute and NoCacheAttribute to your Solution to decorate your methods or classes (you can use the existing attributes defined in MethodCache.Attributes).

## Example

DictionaryCache (in-memory implementation):

	public class DictionaryCache
	{
		public DictionaryCache()
		{
			Storage = new Dictionary<string, object>();
		}

		private Dictionary<string, object> Storage { get; set; }

		// Note: The methods Contains, Retrieve, Store (and Remove) must exactly look like the following:

		public bool Contains(string key)
		{
			return Storage.ContainsKey(key);
		}

		public T Retrieve<T>(string key)
		{
			return (T)Storage[key];
		}

		public void Store(string key, object data)
		{
			Storage[key] = data;
		}

		// Remove is needed for writeable properties which must invalidate the Cache
		// You can skip this method but then only readonly properties are supported
		public void Remove(string key)
		{
			Storage.Remove(key);
		}
	}

Now all the preparation is done and you can start with the real work. The classes you want to cache must contain an Cache Getter (can also be inherited from a baseclass). Let's start decorating ...

	// Mark the class to enable caching of every method ...

	[Cache]
	public class ClassToCache
	{
		public ClassToCache()
		{
			// Consider using constructor or property injection instead
			Cache = new DictionaryCache();
		}

		// Consider using ICache Interface
		private DictionaryCache Cache { get; set; }

		// This method will be cached
		public int Add(int a, int b)
		{
			return a + b;
		}
		
		// This method will be cached too
		public string Concat(string a, string b)
		{
			return a + b;
		}   
		
		// This method will not be cached
		[NoCache]
		public int ThirdMethod(int x)
		{
			return x * x;
		}  
		
		public string AlsoWorksForProperties
		{
			get
			{
				return DoSomeCalculations();
			}
		}
		
		[NoCache]
		public string AlsoPropertiesCanBeIgnored
		{
			get
			{
				return 10;
			}
		}
	}

	// or mark the methods you want to cache explicitly.

	public class ClassToCache
	{
		public ClassToCache(ICache cache)
		{
			// Consider using constructor or property injection instead
			Cache = new DictionaryCache();
		}

		private ICache Cache { get; set; }

		[Cache] // Only this method ...
		public int Add(int a, int b)
		{
			return a + b;
		}

		public string Concat(string a, string b)
		{
			return a + b;
		}

		public int ThirdMethod(int x)
		{
			return x * x;
		}

		[Cache] // ... and this properties will be cached
		public string AlsoWorksForProperties
		{
			get
			{
				return DoSomeCalculations();
			}
		}
	}

... and let MethodCache do the rest.

## Miscellaneous

### Choosing what to cache

Class' methods and properties are selected for caching using the following algorithm:

1. For each member
	1. If it is marked with `[Cache]` attribute, **cache it**
	2. If it is marked with `[NoCache]` attribute, **don't cache it**
	2. If it is a method
		2. If it the class is marked with `[Cache(Members.Methods)]` or `[Cache(Members.All)]`, **cache it**
		3. It it the class is marked with `[Cache]` attribute and methods are not disabled in XML config, **cache it**
	3. If it is a property
		1. If it is an auto-property, **don't cache it**
		2. If it is a write-only property, **don't cache it**
		3. If it is the `Cache` property, **don't cache it**
		2. If it the class is marked with `[Cache(Members.Properties)]` or `[Cache(Members.All)]`, **cache it**
		3. It it the class is marked with `[Cache]` attribute and properties are not disabled in XML config, **cache it**
	4. Otherwise, **don't cache it**

For disabling method or property caching per default (say you only want to cache methods when using `[Cache]` on class level) edit the XML settings in ModuleWeavers.xml:

`<MethodCache CacheMethods="true" CacheProperties="false" />`

Notes
* `[Cache(Members.Methods | Members.Properties)]` can be used in place of `[Cache(Members.All)]`.
* Arguments passed to `[Cache]` attribute will be ignored on individual members

### Improvements

For production I would suggest using some DI framework and creating an ICache interface:

	public interface ICache
	{
		bool Contains(string key);

		T Retrieve<T>(string key);

		void Store(string key, object data);
		
		void Remove(string key);
	}
	
	public class MyService
	{
		// Constructor injection
		public MyService(ICache cache)
		{
			Cache = cache;
		}
		
		protected ICache Cache { get; set; }
	}

### Runtime Debug messages

When compiled in Debug mode, MethodCache outputs Cache information with Debug.WriteLine:

	CacheKey created: MethodCache.TestAssembly.TestClass1.MethodOne_1337
	Storing to cache.
	CacheKey created: MethodCache.TestAssembly.TestClass1.MethodOne_1337
	Loading from cache.
	...

If you do not like this behaviour (e.g. if lots of messages slow down the application), add SkipDebugOutput="false" to ModuleWeavers.xml.

### Enable Weaving Build Messages

Be default, only warnings like a missing Cache Getter are shown in the build log. To enable detailed information, modify the following line in Fody.targets

From

	<FodyMessageImportance Condition="$(FodyMessageImportance) == '' Or $(FodyMessageImportance) == '*Undefined*'">Low</FodyMessageImportance>

To

	<FodyMessageImportance Condition="$(FodyMessageImportance) == '' Or $(FodyMessageImportance) == '*Undefined*'">High</FodyMessageImportance>

You will now see detailed weaving information in the build log:

	Searching for Methods in assembly (MethodCache.TestAssembly.dll).
	Weaving method TestClassOne::MethodOne.
	Checking CacheType methods (Contains, Store, Retrieve).
	CacheType methods found.
	...

### Credits

Thanks to [Tomasz Pluskiewicz](https://github.com/tpluscode) for his contribution to support caching of properties.