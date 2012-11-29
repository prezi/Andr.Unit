//
// Copyright 2011-2012 Xamarin Inc.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//   http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
using System;

using Android.App;
using Android.Content;
using Android.Views;
using NUnit.Framework.Internal;

using NUnitLite;
using NUnit.Framework.Api;
using NUnitLite.Runner;

namespace Android.NUnitLite.UI
{
	class TestCaseElement : TestElement
	{
		
		public TestCaseElement (NUnit.Framework.Internal.Test test) : base (test)
		{
			if (test.RunState == RunState.Runnable)
				Indicator = "..."; // hint there's more
		}
		
		protected override string GetCaption ()
		{
			if (Result == null) {
				return String.Format ("<b>{0}</b><br><font color='white'>{1}</font>", TestCase.Name, TestCase.RunState);
			} else if (Result.ResultState.Status == TestStatus.Passed) {
				return String.Format ("<b>{0}</b><br><font color='lime'>Success!</font>", TestCase.Name); 
			} else if (Result.ResultState.Status == TestStatus.Failed) {
				return String.Format ("<b>{0}</b><br><font color='red'>{1}</font>", TestCase.Name, Result.Message); 
			} else if (Result.ResultState.Status == TestStatus.Inconclusive) {
				return String.Format ("<b>{0}</b><br><font color='#FF00FF'>{1}</font>", TestCase.Name, Result.Message); 
			} else { //skipped or ignored
				return String.Format ("<b>{0}</b><br><font color='yellow'>{1}: {2}</font>", TestCase.Name, TestCase.RunState, Result.Message);
			}
		}
		
		public NUnit.Framework.Internal.Test TestCase {
			get { return Test as NUnit.Framework.Internal.Test; }
		}
		
		public override View GetView (Context context, View convertView, ViewGroup parent)
		{
			View view = base.GetView (context, convertView, parent);
			view.Click += delegate {
				if (TestCase.RunState != RunState.Runnable)
					return;
								
				AndroidRunner runner = AndroidRunner.Runner;
				if (!runner.OpenWriter ("Run " + TestCase.FullName, context))
					return;
				
				try {
					//Test.Run (runner);
					runner.Run (TestCase);
				} finally {
					runner.CloseWriter ();
				}

				if (Result.ResultState.Status != TestStatus.Passed) {
					Intent intent = new Intent (context, typeof(TestResultActivity));
					intent.PutExtra ("TestCase", Name);
					intent.AddFlags (ActivityFlags.NewTask);			
					context.StartActivity (intent);
				}
			};
			return view;
		}
	}
}