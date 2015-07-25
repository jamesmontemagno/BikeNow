using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Xamarin.UITest;
using Xamarin.UITest.Android;
using Xamarin.UITest.Queries;

namespace BikeNow.UITests
{
    [TestFixture]
    public class Tests
    {
        AndroidApp app;

        [SetUp]
        public void BeforeEachTest()
        {
            // TODO: If the Android app being tested is included in the solution then open
            // the Unit Tests window, right click Test Apps, select Add App Project
            // and select the app projects that should be tested.
            app = ConfigureApp
                .Android.ApkFile ("../../../APK/Test/com.refractored.bikenowpronto.apk")
                .StartApp();
        }

        [Test]
        public void AppLaunches()
        {
            app.Screenshot("First screen.");
        }

        [Test]
        public void OpenNav()
        {
            app.Screenshot("First screen.");
            app.Tap(x => x.Marked("Open drawer"));
            app.Screenshot("Open Drawer.");
            app.Tap(x => x.Marked("map"));
        }

        [Test]
        public void TapMap()
        {
            app.Screenshot("First screen.");
            app.Tap(x => x.Marked("map"));
            app.Screenshot("Map Tapped.");
        }
    }
}

