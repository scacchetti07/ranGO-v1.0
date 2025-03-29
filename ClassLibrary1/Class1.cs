namespace ClassLibrary1;

public static class Class1
{
    public static string Teste { get; private protected set; }

    public static void TesteMetodo()
    {
        Teste = "Teste";
    }
}

public static class Class2
{
    public static void TesteMtodo2()
    {
        Class1.Teste = "";
    }
}