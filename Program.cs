namespace com.github.kbinani.feztradenotify {
    class Program {
        static void Main( string[] args ) {
            var runner = new DaemonRunner();
            runner.Run();
        }
    }
}
