Phase Shift Career Extension is an extension to Phase Shift which adds the ability to add career story line
content. Although Phase Shift does have the concept of tiers, unlike games in the RockBand series, it does
not provide any career storyline along with those tiers. This extension attempts to remedy that by detecting
when the player has advanced from one tier to another and then display some tier related content if available
to the user. By having the content, for each tier, follow a story line it is possible to use this to provide
a career story as the player transitions through the tiers.

Phase Shift Career Extension comes in two parts. The first part, Phase Shift Tier Plugin, is responsible for
detecting tier changes. Since the Phase Shift source code is not available this was accomplished by some
reverse engineering of the tier ini files and the user dat files. The second part creates a form with a mini
web browser page which can be used to render HTML content. This allows the (tier) story line content to use
a variety of media (basically anything the web browser control supports).

Video Link: https://github.com/LordAshes/PhaseShiftCareerExtension

FEATURES:

- Can support tiers for all the main instrument types: bass, drums, guitar, and keys.
- Optimization checks only files related to the current career and band once detected.
- Supports any number of tiers (i.e. simple and complex story lines).
- Allows the visualized content to be customized to the band name.
- Content visualization allows the use of any HTML (including text, pictures, sound and video) supported by
  the web browser control.

LIMITATIONS:

Since the Phase Shift source code is not available this extension is purely an independent application which
monitors some Phase Shift files in order to figure out which career and band the player is using and when
the user has transitioned between tiers. Since Phase Shift defines tiers for each instrument type and there
is not obvious way for the extension to determine which instrument the player is playing, the user needs to
provide the instrument type (on which the tiers are based) on startup.

Due to the optimization process, one the extension detects which career and band is being used, if the player
wishes to switch careers or band, the extension needs to be restarted.

STARTUP SYNTAX:

PhaseShiftTierPlugin instrument contentVisualizer

Where "instrument" is one of the following key words: bass, drums, guitar, or keys.
Where "contentVisualizer" is an application which takes the tier change info and does something with it.
In this case the plugin calls PhaseShiftStoryboardView which is a Chrome based web browser capable of
rendering the tier change associated content. The PhaseShiftStoryboardView is based on CefShart's
Cromium based browser because the regular C# web browser control does not properly render some/all HTML5
content (such as the rotation of text used to place the band name into the content).

CAREER SAMPLE:

Included in the Careers folder is a simple sample career storyline for a Rockband Beatles career.
For copyright reasons the actual songs are not included but the sample shows how to create the
html content which forms the story line. The sample also shows how to extract the band name and
add it to the custom content.
