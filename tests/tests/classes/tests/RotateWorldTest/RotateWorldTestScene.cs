using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CocosSharp;

namespace tests
{
    public class RotateWorldTestScene : TestScene
    {
        protected override void NextTestCase()
        {
        }
        protected override void PreviousTestCase()
        {
        }
        protected override void RestTestCase()
        {
        }
        public override void runThisTest()
        {
            CCLayer pLayer = RotateWorldMainLayer.node();
            AddChild(pLayer);
            RunAction(new CCRotateBy (4, -360));
            CCApplication.SharedApplication.MainWindowDirector.ReplaceScene(this);
        }
    }
}
