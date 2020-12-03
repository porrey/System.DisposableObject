namespace Sample
{
	class Program
	{
		static void Main(string[] args)
		{
			using (SomeObject obj1 = new SomeObject())
			{
				// ***
				// *** obj will be disposed and the dispose
				// *** methods will be called.
			}

#if (NET5_0)
			using (SomeAsyncObject obj2 = new SomeAsyncObject())
			{
				// ***
				// *** obj will be disposed and the dispose
				// *** methods will be called.
			}
#endif
		}
	}
}
