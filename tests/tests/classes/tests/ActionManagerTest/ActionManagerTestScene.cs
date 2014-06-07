using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CocosSharp;

namespace tests
{
    public class ActionManagerTestScene : TestScene
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
            CCLayer pLayer = ActionManagerTest.nextActionManagerAction();
            AddChild(pLayer);

            CCApplication.SharedApplication.MainWindowDirector.ReplaceScene(this);
        }
    }
}
