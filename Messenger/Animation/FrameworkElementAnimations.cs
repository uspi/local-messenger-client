﻿using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Animation;

namespace Messenger
{
    /// <summary>
    /// Helpers to animate <see cref="FrameworkElement"/> in specific ways
    /// </summary>
    public static class FrameworkElementAnimations
    {
        /// slides a element in FROM the RIGHT
        /// <param name="keepMargin">Whether to keep the element at the same
        /// width during animation</param>
        public static async Task SlideAndFadeInFromRightAsync(
            this FrameworkElement element, float seconds = 0.3f, bool keepMargin = true)
        {
            // create storyboard
            var sb = new Storyboard();

            // add slide from right animaiton
            sb.AddSlideFromRight(seconds, element.ActualWidth, keepMargin: keepMargin);
            
            // add fade in animation
            sb.AddFadeIn(seconds);

            // start animating
            sb.Begin(element);

            // make element visible
            element.Visibility = Visibility.Visible;

            // wait for animation to finish
            await Task.Delay((int)(seconds * 1000));
        }

        /// slides a element in FROM the LEFT
        /// <param name="keepMargin">Whether to keep the element at the same
        /// width during animation</param>
        public static async Task SlideAndFadeInFromLeftAsync(
            this FrameworkElement element, float seconds = 0.3f, bool keepMargin = true)
        {
            // create storyboard
            var sb = new Storyboard();

            // add slide from right animaiton
            sb.AddSlideFromLeft(seconds, element.ActualWidth, keepMargin: keepMargin);

            // add fade in animation
            sb.AddFadeIn(seconds);

            // start animating
            sb.Begin(element);

            // make element visible
            element.Visibility = Visibility.Visible;

            // wait for animation to finish
            await Task.Delay((int)(seconds * 1000));
        }

        /// slides a element out TO the LEFT    
        /// <param name="keepMargin">Whether to keep the element at the same
        /// width during animation</param>
        public static async Task SlideAndFadeOutToLeftAsync(
            this FrameworkElement element, float seconds = 0.3f, bool keepMargin = true)
        {
            // create storyboard
            var sb = new Storyboard();

            // add slide from right animaiton
            sb.AddSlideToLeft(seconds, element.ActualWidth, keepMargin: keepMargin);

            // add fade in animation
            sb.AddFadeOut(seconds);

            // start animating
            sb.Begin(element);

            // make element visible
            element.Visibility = Visibility.Visible;

            // wait for animation to finish
            await Task.Delay((int)(seconds * 1000));
        }

        /// slides a element out TO the RIGHT
        /// <param name="keepMargin">Whether to keep the element at the same
        /// width during animation</param>
        public static async Task SlideAndFadeOutToRightAsync(
            this FrameworkElement element, float seconds = 0.3f, bool keepMargin = true)
        {
            // create storyboard
            var sb = new Storyboard();

            // add slide from right animaiton
            sb.AddSlideToRight(seconds, element.ActualWidth, keepMargin: keepMargin);

            // add fade in animation
            sb.AddFadeOut(seconds);

            // start animating
            sb.Begin(element);

            // make element visible
            element.Visibility = Visibility.Visible;

            // wait for animation to finish
            await Task.Delay((int)(seconds * 1000));
        }
    }
}
