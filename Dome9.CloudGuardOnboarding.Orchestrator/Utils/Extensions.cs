using System;
using System.Collections.Generic;
using System.Text;

namespace Dome9.CloudGuardOnboarding.Orchestrator
{
    public static class Extensions
    {

        /// <summary>
        /// Use only for logging.
        /// Replace all but the last X chars of the string with the specified char. For example MaskChars("password", 4) yields "****word"
        /// </summary>
        /// <param name="sensitiveString"></param>
        /// <param name="numberOfLastCharsToDisplay"></param>
        /// <param name="charToMaskWith"></param>
        /// <returns></returns>
        public static string MaskChars(this string sensitiveString, int numberOfLastCharsToDisplay, char charToMaskWith = '*')
        {
            if (numberOfLastCharsToDisplay < 0 || string.IsNullOrWhiteSpace(sensitiveString))
            {
                return "**MASK*ERROR**";
            }

            numberOfLastCharsToDisplay = Math.Min(numberOfLastCharsToDisplay, sensitiveString.Length);
            var result = new String(charToMaskWith, sensitiveString.Length - numberOfLastCharsToDisplay) + sensitiveString.Substring(sensitiveString.Length - numberOfLastCharsToDisplay);

            return result;
        }
    }
}
