﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Threading;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Testing;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Tests.Visual.Drawables
{
    public class TestSceneModelBackedDrawable : TestScene
    {
        private TestModelBackedDrawable backedDrawable;

        private void createModelBackedDrawable(bool hasIntermediate) =>
            Child = backedDrawable = new TestModelBackedDrawable
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Size = new Vector2(200),
                HasIntermediate = hasIntermediate,
            };

        [Test]
        public void TestEmptyDefaultState()
        {
            AddStep("setup", () => createModelBackedDrawable(false));
            AddAssert("nothing shown", () => backedDrawable.DisplayedDrawable == null);
        }

        [Test]
        public void TestModelDefaultState()
        {
            TestDrawableModel drawableModel = null;

            AddStep("setup", () =>
            {
                createModelBackedDrawable(false);
                backedDrawable.Model = new TestModel(drawableModel = new TestDrawableModel(1).With(d => d.AllowLoad.Set()));
            });

            assertDrawableVisibility(1, () => drawableModel);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void TestChangeModel(bool hasIntermediate)
        {
            TestDrawableModel firstModel = null;
            TestDrawableModel secondModel = null;

            AddStep("setup", () =>
            {
                createModelBackedDrawable(hasIntermediate);
                backedDrawable.Model = new TestModel(firstModel = new TestDrawableModel(1).With(d => d.AllowLoad.Set()));
            });

            AddStep("set second model", () => backedDrawable.Model = new TestModel(secondModel = new TestDrawableModel(2)));
            assertIntermediateVisibility(hasIntermediate, () => firstModel);

            AddStep("allow second model to load", () => secondModel.AllowLoad.Set());
            assertDrawableVisibility(2, () => secondModel);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void TestChangeModelDuringLoad(bool hasIntermediate)
        {
            TestDrawableModel firstModel = null;
            TestDrawableModel secondModel = null;
            TestDrawableModel thirdModel = null;

            AddStep("setup", () =>
            {
                createModelBackedDrawable(hasIntermediate);
                backedDrawable.Model = new TestModel(firstModel = new TestDrawableModel(1).With(d => d.AllowLoad.Set()));
            });

            AddStep("set second model", () => backedDrawable.Model = new TestModel(secondModel = new TestDrawableModel(2)));
            assertIntermediateVisibility(hasIntermediate, () => firstModel);

            AddStep("set third model", () => backedDrawable.Model = new TestModel(thirdModel = new TestDrawableModel(3)));
            assertIntermediateVisibility(hasIntermediate, () => firstModel);

            AddStep("allow second model to load", () => secondModel.AllowLoad.Set());
            assertIntermediateVisibility(hasIntermediate, () => firstModel);

            AddStep("allow third model to load", () => thirdModel.AllowLoad.Set());
            assertDrawableVisibility(3, () => thirdModel);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void TestOutOfOrderLoad(bool hasIntermediate)
        {
            TestDrawableModel firstModel = null;
            TestDrawableModel secondModel = null;

            AddStep("setup", () =>
            {
                createModelBackedDrawable(hasIntermediate);
                backedDrawable.Model = new TestModel(firstModel = new TestDrawableModel(1));
            });

            AddStep("set second model", () => backedDrawable.Model = new TestModel(secondModel = new TestDrawableModel(2)));
            assertIntermediateVisibility(hasIntermediate, () => null);

            AddStep("allow second model to load", () => secondModel.AllowLoad.Set());
            assertDrawableVisibility(2, () => secondModel);

            AddStep("allow first model to load", () => firstModel.AllowLoad.Set());
            assertDrawableVisibility(2, () => secondModel);
        }

        [Test]
        public void TestSetNullModel()
        {
            TestDrawableModel drawableModel = null;

            AddStep("setup", () =>
            {
                createModelBackedDrawable(false);
                backedDrawable.Model = new TestModel(drawableModel = new TestDrawableModel(1).With(d => d.AllowLoad.Set()));
            });

            assertDrawableVisibility(1, () => drawableModel);

            AddStep("set null model", () => backedDrawable.Model = null);
            AddAssert("nothing shown", () => backedDrawable.DisplayedDrawable == null);
        }

        private void assertIntermediateVisibility(bool hasIntermediate, Func<Drawable> getLastFunc)
        {
            if (hasIntermediate)
                AddAssert("no drawable visible", () => backedDrawable.DisplayedDrawable == null);
            else
                AddAssert("last drawable visible", () => backedDrawable.DisplayedDrawable == getLastFunc());
        }

        private void assertDrawableVisibility(int id, Func<Drawable> getFunc)
        {
            AddAssert($"model {id} visible", () => backedDrawable.DisplayedDrawable == getFunc());
        }

        private class TestModel
        {
            public readonly TestDrawableModel DrawableModel;

            public TestModel(TestDrawableModel drawableModel)
            {
                DrawableModel = drawableModel;
            }
        }

        private class TestDrawableModel : CompositeDrawable
        {
            public readonly ManualResetEventSlim AllowLoad = new ManualResetEventSlim(false);

            public TestDrawableModel(int id)
                : this($"Model {id}")
            {
            }

            protected TestDrawableModel(string text)
            {
                RelativeSizeAxes = Axes.Both;

                InternalChildren = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = Color4.SkyBlue
                    },
                    new SpriteText
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Text = text
                    }
                };
            }

            [BackgroundDependencyLoader]
            private void load()
            {
                if (!AllowLoad.Wait(TimeSpan.FromSeconds(10)))
                {
                }
            }
        }

        private class TestModelBackedDrawable : ModelBackedDrawable<TestModel>
        {
            protected override Drawable CreateDrawable(TestModel model) => model.DrawableModel;

            public new Drawable DisplayedDrawable => base.DisplayedDrawable;

            public new TestModel Model
            {
                set => base.Model = value;
            }

            public bool HasIntermediate;

            protected override bool TransformImmediately => HasIntermediate;
        }
    }
}
