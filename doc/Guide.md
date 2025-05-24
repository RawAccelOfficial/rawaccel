# Raw Accel Guide
## Download

Visit the [Releases page](https://github.com/a1xd/rawaccel/releases) and navigate to the "Assets" dropdown under the latest release, then download the zip file titled `RawAccel_X.X.X.zip` where X are the latest version numbers. **DO NOT** download by clicking the green "Code"/"Download" file on the home page of this repository as that will only download the source code, which isn't what you want.

## Installation
- **Prerequisites**
  * Windows 10 or Windows 11
  * Visual C++ 2019 runtime, [download here](https://aka.ms/vs/16/release/vc_redist.x64.exe)
  * .NET Framework 4.7.2+ runtime, [download here](https://dotnet.microsoft.com/download/dotnet-framework/net48)

- Run `installer.exe` in the release directory to install the Raw Accel driver. Restart your computer for the installation to take effect.

- Run `uninstaller.exe` in the release directory to uninstall the driver. Restart for the uninstallation to take effect.

- Run `rawaccel.exe` when the driver is installed in order to run the Raw Accel GUI.

## Philosophy
The Raw Accel driver and GUI's workings and exposed parameters are based on our understanding of mouse acceleration. Our understanding includes the concepts of "[gain](#gain-switch)", "[whole vs by component](#horizontal-and-vertical)", and "[anisotropy](#anisotropy)." For clarity, we will outline this understanding here. Those uninterested can skip to [Features](#features) below.

### Measurements from Input Speed
Raw Accel, like any mouse modification program, works by acting on a passed-in $(x,y)$ input in order to pass back out an $(x,y)$ output. The GUI program creates charts by feeding a set of $(x,y)$ inputs and times to the driver code to receive a set of $(x,y)$ outputs. The following measurements, available as charts in Raw Accel, are then found from the outputs:

- **Sensitivity**: The ratio of the output speed to the input speed. The "sensitivity multiplier" parameter in the program is a multiplier used on the post-calculation output vector.
- **(Output) Velocity**: The speed of the final output vector. The output vs input velocity curve is perhaps the most important relationship in a particular setup because it directly describes the output for any given input. (We use "speed" and "velocity" interchangeably, and are aware of the difference elsewhere.)
- **Gain**: The slope of the output vs input velocity curve. It answers the question: "if I move my hand a little faster, how much faster will my cursor move?" The relationship between gain and sensitivity is that if gain is continuous, so is sensitivity. The reverse is not necessarily true, so keeping the gain "nice" ensures "nice" sensitivity but not vice versa.
- For the mathematically inclined: for input speed $v$ and Output Velocity $f(v)$, Sensitivity is $f(v)/v$ and Gain is $f'(v) = \frac{\mathrm d}{\mathrm d v} \left ( f(v) \right)$.

Acceleration, then, is a characteristic of the velocity curve, defined as true when the velocity curve is non-linear for any input speed.

### Example
The above is much more clear with an example. Let's say I have 
- linear acceleration with acceleration parameter of 0.01
- a sensitivity parameter of 0.5

and I move my mouse to create an input of $(30,40)$ at a poll rate of 1000 hz.

Then our input speed is $\sqrt{30^2 + 40^2} = 50$ counts/ms. Our accelerated sensitivity is calculated to be $(1 + 0.01 * 50) * 0.5 = 1.5 * 0.5 = 0.75$. So our output velocity is $0.75 * 50  = 37.5$. If I run the previous calculations with input speed 49.9 I get output velocity 37.40005, so our gain is about $\frac{37.5-37.40005}{50-49.9} = 0.9995$. Here is a picture of the charts in Raw Accel showing the same thing:

![SensVelocityGainExample](images/accel_readme_example.png)

### Coalescion
Mouse packets in theory arrive every poll time, but in practice sometimes are not so evenly spaced. For instance, if a user moves their hand exactly 30 counts/ms horizontally over three poll times of 1ms, the expected packets in would come in like: (30, 0), wait time of 1 ms, (30, 0), wait time of 1ms, (30, 0). But in practice, sometimes these packets may come in like: (28, 0), 1.2 ms wait time, (35, 0), 0.8 ms wait time, (27, 0).  
Coalescion allows averaging of the current and previous packets to mitigate the possibility of unevenness. There are different points in Raw Accel's packet processing where this averaging can occur:  
- When determining input speed for sensitivity. Uses linear EMA. Only affects acceleration and therefore does not feel like classic "mouse smoothing."  
- When determining accelerated sensitivity. Uses simple EMA. Only affects acceleration and therefore does not feel like classic "mouse smoothing."  
- When determining output. Uses linear EMA. Effects output directly and therefore is and feels like classic "mouse smoothing".  

"Classic mouse smoothing" is a setting present in many programs that take mouse input, such as games. This setting smooths the mouse packets together directly, which has the affect of adding an input delay between your physical mouse and cursor reaching the target, as well as making the connection between the two less direct. Only "output smoothing" in Raw Accel is like this. Input and sensitivity smoothing affect only the values used to determine the amount of acceleration.  

Exponential moving average, or EMA, is the algorithm by which packets are averaged together. For each packet, an existing moving average undergoes exponential decay and then is averaged with the information from the new packet. The user can give the half-life for the decay of the moving average. Linear EMA adds a moving average for the trend (rate of change) as well. More info can be found about these algorithms online.


### Horizontal and Vertical
Due to the mechanics of holding a mouse on a desk, users generally move their mouses horizontally (left and right) differently than they move them vertically (forward and back), with more freedom for the wrist and\or arm to move the mouse horizontally than vertically. A user may then desire for various aspects of their output to change based on the direction of their input. For settings which allow this we have co-opted the term "anisotropy", which is "the property of a material which allows it to change or assume different properties in different directions."

In the above "[Example](#example)" section, the $x$ and $y$ inputs are not treated separately; rather they are "combined" by using the magnitude if the input vector: $\sqrt{30^2 + 40^2} = 50$ counts/ms. This is called "Whole" application because the whole speed of the input is used and the result is applied to the whole vector. Application styles include:

#### ***Whole***
In this case, the magnitude of the input vector is input to sensitivity calculation, and applied to whole vector, as in example above.

$$(out_x, out_y) = (in_x * sens_x, in_y * sens_y) * f(\sqrt{in_x^2 + in_y^2})$$, where $f(v)$ is our sensitivity function

Separate horizontal and vertical sensitivites still feel great in this mode. (For the mathematically inclined, that's because differing horizontal and vertical sensitivities create a vector field without curl or other oddities.)

There are anisotropic settings for whole mode.  
- **Range**. This scales the range of the sensitivity curve around 1 for the horizontal or vertical direction.  
    - If a given curve varies from 1 to 2 sensitivity, then a $range_y$ of 0.5 would mean that vertical movements vary from 1 to 1.5 sensitivity instead.  
- **Domain**. This scales the domain of curve around 0 for the horizontal or vertical direction.  
    - If a given curve has an offset at 5 count/ms and a cap that is hit at 15 counts/ms, then a $domain_y$ of 2 would mean that vertical movements hit the offset at 2.5 counts/ms and the cap at 7.5 counts/ms instead.  
- **Lp Norm**. The distance calculation can be generalized to $((in_x)^p + (in_y)^p)^\frac{1}{p}$, bringing the calculation into [Lp space](https://en.wikipedia.org/wiki/Lp_space).  
    - $p = 2$ is then the "real world" value, yielding the pythagorean theorem as the distance calculation.  
    - Increasing $p$ makes distances for diagonal movements (where $in_x$ and $in_y$ are close) smaller, and increases the dominance of the larger of the two in determining the distance.  
    - As $p$ gets large, the above calculation approaches $\max(in_x, in_y)$. Raw Accel uses this formula when given any $p > 64$.
    - We recommend almost everyone leave this at 2.  

![AnisotropyExample](images/anisotropy_example.png)

With all anisotropic settings considered, the full formula looks like:  

$$(out_x, out_y) = (in_x * sens_x, in_y * sens_y) \left(\left(f\left(\left((domain_x * in_x)^p + (domain_y * in_y)^p\right)^\frac{1}{p}\right) - 1\right) \left(\frac{2}{\pi} \arctan(| \frac{in_y}{in_x} |) (range_y - range_x) + range_x\right) + 1\right)$$, where $f(v)$ is our sensitivity function

This can be more easily understood as  

$$(out_x, out_y) = (in_x * sens_x, in_y * sens_y) * \left(\left(f\left( \text{domain-weighted } L^p \text{ space speed}\right) - 1\right) * (\text{directional weight}) + 1\right)$$, where $f(v)$ is our sensitivity function

This formula guarantees the smooth transition from the horizontal to vertical curve and vice versa as the user moves their hand diagonally.

#### ***By Component***  
In this case, the horizontal components are separated and each is given as input to the sensitivity calculation to multiplied by itself before being recombined at output.

$$(out_x, out_y) = (in_x * f(in_x) * sens_x, in_y * f(in_y) * sens_y))$$

You can also do:

$$(out_x, out_y) = (in_x * f(in_x) * sens_x, in_y * g(in_y) * sens_y))$$, where $g(v)$ is some other sensitivity function.

All anisotropic effects for By Component mode can be achieved by setting different $x$ and $y$ curves.  

The authors of this program feel that Whole is the best style for most users, but that users who play games which have very separate horizontal and vertical actions to manage (such as tracking an enemy and controlling recoil) might benefit from trying By Component. By Component may seem more flexible, but it is precisely the restrictions imposed by Whole (no curl) that make it smoother.
  
  
## Features

### Sensitivity Multiplier
As described above, the "sensitivity multiplier" parameter is a multiplier used on the post-calculation output vector. The "Y/X Ratio" parameter is then only applied to the Y component of the output, so that it defines the ratio of vertical to horizontal output sensitivity without acceleration.

### Gain Switch
The acceleration curve styles below (see "[Acceleration Styles](#acceleration-styles)") each describe a certain shape mathematically. The gain switch determines whether that shape is applied in the sensitivity graph or the gain graph. For styles [Linear](#linear), [Classic](#classic), and [Power](#power), this setting does not change the possible shapes of the velocity curve - that is, for any particular settings with the gain switch set to Sensitivity, there is a different set of settings that will replicate the exact same velocity curve (output for a given hand motion) with the switch set to Gain. For styles [Natural](#natural), [Jump](#jump), and [Synchronous](#synchronous), this is not true, and the gain switch allows new velocity curves for each style. 

### Offsets
An offset, sometimes called a threshold, is a speed in counts before acceleration "kicks in". The legacy way of applying an offset is having a multiplier of 1 below and at the offset, and applying the sensitivity of (speed-offset) above. This legacy "sensitivity offset" is not available because it causes a discontinuity in gain at the point of offset, leading to non-smooth feeling at offset cross. The new "gain offset" does a little extra math to simply shift the gain graph by the offset amount without any discontinuity. This feels smoother and has almost no effect on sensitivity. The theory behind "gain offsets" is developed in [this document](https://docs.google.com/document/d/1P6LygpeEazyHfjVmaEygCsyBjwwW2A-eMBl81ZfxXZk).

Offsets are only applicable to the [Classic](#classic), [Linear](#linear), and [Natural](#natural) modes, where they are defined in terms of an input speed. Power mode has a special "output offset", where the curve "starts from" some ratio of the sens multiplier, described in its section.

### Caps
A cap is a point after which acceleration is not applied. The legacy way of applying an offset is simply applying the minimum of the cap sensitivity and the calculated sensitivity for any acceleration calculation. Thus, for the legacy "sensitivity cap" the value given is a sensitivity. This cap style is still available but causes a large discontinuity at the point of offset, leading to a non-smooth feeling at cap cross. The new default "gain cap" effectively caps the gain, but for internal calculation reasons, does so for a given speed rather than a given gain value. This feels much smoother but might have a large effect on sensitivity as gain generally raises faster than sensitivity. We recommend that users use a gain cap and simply adjust it to hit at the gain equivalent to the sensitivity they'd like to cap at. The theory behind "gain caps" is developed in [this document](https://docs.google.com/document/d/1FCpkqRxUaCP7J258SupbxNxvdPfljb16AKMs56yDucA).

Caps are only applicable to the [Classic](#classic), [Linear](#linear), and [Power](#power) modes. The capping point can be defined in terms of an input speed, an output ratio, or both (which will then set other acceleration parameters for the mode.)

### Anisotropy
See "[Horizontal and Vertical](#horizontal-and-vertical)" in the philosophy section to understand what these options do.

### Last Mouse Move
The Raw Accel GUI reads the output of the raw input stream, and thus the output of the Raw Accel Driver, and displays on the graphs red points corresponding to the last mouse movements. These calulations should be fast and your graph responsive, but it comes at the cost of higher CPU usage due to needing to refresh the graph often. This feature can be turned off in the "Charts" menu.

### Input coalescion
See "[Coalescion](#coalescion)" in the philosophy section to understand what these options do. These settings are currently exposed only in the settings file, for now, as "Time in ms after which [setting name] is weighted at half its original value".

### Menu Options

#### Charts >> Scale by DPI and Poll Rate
These options does not scale your acceleration curve in any way. Rather, DPI scales the set of points used to graph your curve, and shows you a window of input speed relevant for your chosen DPI. The poll rate is used as a safeguard for the Last Mouse Move points and therefore should be set for accuracy in that measurement.

#### Advanced >> Device Menu
This menu provides options for individually disabling devices, and normalizing device DPI (see next section). Here you will also find an option for setting polling rate, which signals the driver to forgo the standard automatic rate adjustment. Leave this at 0 unless you are experiencing cursor stutters that only occur with acceleration enabled.

#### DPI Normalization
Setting the DPI option for a device to its actual DPI will scale its input so that the sensitivity and acceleration feels as if it were set to 1000 dpi. For example, with a sens multiplier of 0.8, mice with their DPI set in the device menu will have the equivalent sensitivity of an 800 DPI mouse with a sens multiplier of 1. Ignoring device-specific factors like weight, friction, and sensor position, normalization provides an easy method to synchronize settings across different setups.

This is still an experimental setting, which perhaps will be more clearly presented in the future. For now, users should set their sensitivity multiplier to no greater than their DPI divided by 1000 to avoid pixel skipping on desktop (with 6/11 window sensitivity.)

## Acceleration Styles
The examples of various types below show some typical settings for a mouse at, or normalized to, 1000 DPI.

### Synchronous
This accel type is what we think is most "correct". It achieves a (logarithmically symmetrical) change in sensitivity around a speed called the "synchronous speed". The idea is that we perceive differences in sensitivity and hand speed by proportion (i.e. logarithmically) instead of linearly, and also that we have some central or anchor speed we use for estimating changes. Hence, making a logarithmically symmetrical sensitivity change around the central speed causes that change to be in sync with our natural estimating. 
The most important variable is the synchronous speed. If this value is correct, then the curve will be estimable in use for any reasonable values of the other variables.
The motivity variable expresses how much change will occur. Since the change is proportionally symmetric, the curve will start at sensitivity "1/motivity" and end at "motivity".
The gamma variable expresses how fast the change occurs. It is equivalent to the "exponent" value in Power mode (as Power mode can be thought of as a synchronous curve with infinite motivity.)
Lastly, "smooth" affects how fast the changes tails in and out. We recommend leaving this at default value of 0.5 for some mathematical reasons (corresponds to tanh() function). Value of 0 causes instant tailing in and out - makes the "s" of the curve into a "z".
![SynchronousExample](images/synchronous_example.png)

### Linear
This is simplest style; it is simply a line rising at a given rate.
![LinearExample](images/linear_example.png)

### Classic
This is the style found in Quake 3, Quake Live, and countless inspired followers, including the InterAccel program. It multiplies the speed by a given rate and then raises the product to a given exponent. Any particular linear style curve can be replicated in classic style with an exponent of 2.
![ClassicExample](images/classic_example.png)

### Power
This is the style found in CS:GO and Source Engine games (m_customaccel 3). The user can set a rate by which the speed is multiplied, and then an exponent to which the product is raised, which is then the final multiplier (no adding to 1). The formula for this curve starts at 0 for an input of 0, so the user can also set a ratio of the sens multiplier for the curve to start at, effectively an output offset.

In the aforementioned games the default m_customaccel_exponent value of 1.05 would be a value of 0.05 in Raw Accel, leading to a concave slowly rising curve. CS:GO and Source Engine games apply acceleration in an fps-dependent manner, so Raw Accel can only simulate acceleration from these games at a given fps. To do so, set scale to 1000/(in-game fps) and the output offset to 1.
![PowerExample](images/power_example.png)

### Natural
Natural features a concave curve which starts at 1 and approaches some maximum sensitivity. The sensitivity version of this curve can be found in the game Diabotical.
![NaturalGainExample](images/natural_gain_example.png)

### Jump
 This style applies one sensitivity or gain below a certain threshold, and another above it. It can be useful for those who want one constant sensitivity and gain for slow hand motions and a different constant sensitivity or gain for fast hand motions. Users can set a "smooth" parameter which dictates whether the jump happens instaneously (at smooth 0) or with a slight tailing in and out (smooth 1) leading to a small sigmoid shape (s-shape). (Note that this "smooth" parameter has nothing to do with averaging over mouse counts like in sensor smoothing on mice or mouse smoothing in games.)
![JumpExample](images/jump_example.png)

### Look Up Table
This curve style is a blank canvas on which to create a curve. It allows the user to define the points which will make up the curve. For this reason, this mode is only for experts who know exactly what they want. Points can be supplied in the GUI according to format x1,y1;x2,y2;...xn.yn or in the settings.json in json format. The default Windows mouse acceleration settings (Enhanced Pointer Precision) can be very closely emulated with this style, using velocity points: "1.505035,0.85549892;4.375,3.30972978;13.51,15.17478447;140,354.7026875;".
![LUTExample](images/LUT_example.png)

## Further Help
Further help and frequently asked questions can be found in the [FAQ](FAQ.md).
