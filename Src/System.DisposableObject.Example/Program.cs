namespace Sample
{
	class Program
	{
		static void Main(string[] args)
		{
			using (SomeObject obj1 = new())
			{
				//
				// obj will be disposed and the dispose
				// methods will be called.
			}

			using (SomeAsyncObject obj2 = new())
			{
				//
				// obj will be disposed and the dispose
				// methods will be called.
			}
		}
	}
}
