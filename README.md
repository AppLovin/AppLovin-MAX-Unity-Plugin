# AppLovin MAX Unity Plugin Demo App

## Overview
MAX is AppLovin's in-app monetization solution.

Move beyond the traditional monetization solution and integrate MAX. MAX is a single unbiased auction where advertisers get equal access to all ad inventory and bid simultaneously, which drives more competition and higher CPMs for you. You can read more about it [here](https://www.applovin.com/max-header-bidding).

To request an invite for MAX, apply [here](https://try.applovin.com/applovin-max-application).

Please check out our [documentation](https://dash.applovin.com/documentation/mediation/unity/getting-started) to get started on integrating and enabling mediated networks using our guides.

## Demo Apps
To get started with the demo apps, follow the instructions below:

1. Clone this project and open the DemoApp project in the Unity Editor.
2. Import the [AppLovin Unity Plugin](https://dash.applovin.com/documentation/mediation/unity/getting-started) files to the project.
3. Under `File > Build Settings`, click on the mobile platform of choice and click on `Player Settings`.
4. Update the iOS bundle identifier and/or Android package name with your own unique identifier(s) associated with the application you will create in the MAX dashboard (or already created, if it is an existing app).
5. Open the `HomeScreen.cs` file under `DemoApp/Assets/Scripts/`.
6. Update the `MaxSdkKey` value with your AppLovin SDK key associated with your account.
7. Update the MAX ad unit IDs for the ad formats you would like to test. Each ad format will correspond to a unique MAX ad unit ID you created in the AppLovin dashboard for the bundle id used before.
8. Under `File > Build Settings`, click on the mobile platform of choice and click `Switch Platform` then `Build` to create a project to test the Demo App.

## Error Codes
| Code          | Description   |
| ------------- |:-------------:|
| -1            | Indicates an unspecified error with one of the mediated network SDKs. |
| 204           | Indicates that no ads are currently eligible for your device. |
| -5001         | Indicates that the ad failed to load due to various reasons (such as no networks being able to fill). |
| -5201         | Indicates an internal state error with the AppLovin MAX SDK. |
| -5601         | Indicates the provided `Activity` instance has been garbage collected while the AppLovin MAX SDK attempts to re-load an expired ad. (Android only) |

## Support
We recommend using GitHub to file issues. For feature requests, improvements, questions or any other integration issues using MAX Mediation by AppLovin, please contact us via our support page: https://monetization-support.applovin.com/hc/en-us.
