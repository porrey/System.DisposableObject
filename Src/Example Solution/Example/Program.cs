namespace Sample
{
	class Program
	{
		static void Main(string[] args)
		{
			using (SomeObject obj = new SomeObject())
			{
				// ***
				// *** obj will be disposed and the dispose
				// *** methods will be called.
			}
		}
	}
}
