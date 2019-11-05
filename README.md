# AppLovin MAX Unity Plugin Demo App

## Overview
MAX is AppLovin's in-app monetization solution.

MAX offers advertisers equal opportunity to bid simultaneously on each impression in a publisher’s inventory via a single unified auction to drive the highest possible yield. You can read more about it [here](https://www.applovin.com/max-header-bidding).

Please check out our [documentation](https://dash.applovin.com/documentation/mediation/unity/getting-started) to get started on integrating and enabling mediated networks using our guides.

## Demo Apps
To get started with the demo apps, follow the instructions below:

1. Clone this project and open the DemoApp project in the Unity Editor.
2. Download the AppLovin Unity Plugin from our [documentation](https://dash.applovin.com/documentation/mediation/unity/getting-started), open the plugin, and import the files to the DemoApp project.
3. Under `File > Build Settings`, click on the mobile platform of choice and click on `Player Settings`.
4. Update the iOS bundle identifier and/or Android package name with your own unique identifier(s) associated with the application you will create in the MAX dashboard (or already created, if it is an existing app).
5. Open the `HomeScreen.cs` file under `DemoApp/Assets/Scripts/`.
6. Update the `MaxSdkKey` value with your AppLovin SDK key associated with your account.
7. Update the MAX ad unit IDs for the ad formats you would like to test. Each ad format will correspond to a unique MAX ad unit ID you created in the AppLovin dashboard for the bundle id used before.
8. Under `File > Build Settings`, click on the mobile platform of choice and click `Switch Platform` then `Build` to create a project to test the Demo App.

## Support
We recommend using GitHub to file issues. For feature requests, improvements, questions or any other integration issues using MAX Mediation by AppLovin, please reach out to your account team and copy devsupport@applovin.com.
