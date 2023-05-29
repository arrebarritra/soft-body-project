---
layout: post
title:  "Final build: collision, interaction, UI and more"
date:   2023-05-29
categories: jekyll update
---

It took a while but I've finally finished the project and I am very satisfied with the result. You can find the Windows binaries [here](https://github.com/arrebarritra/soft-body-project/releases/tag/alpha) if you want to try the demo.

The demo contains multiple scenes, collisions with cubes and interaction with the soft body through movement of cubes as can be seen below:

<p style="text-align:center;">
    <img src="/soft-body-project/img/final_build.gif" alt="Demo of program" width="100%">
    <i>Demo of the program, showing off interaction between cubes and soft body</i>
</p>

One of the biggest challenges I faced was implementing collisions. There are a myriad ways to implement it and most of them are out of the scope of this project. Regardless, I wanted to have collisions as that would show off the soft body physics so much better than a static scene. I finally limited the scope to implementing collisions between cubes and the rigid body, again using [Müller's](https://matthias-research.github.io/pages/publications/posBasedDyn.pdf) approach for collision response. The collision code is also run as a compute shader, and is solved as a constraint similar to the volume and length constraints. If a particle in the soft body is inside a cube, it is simply projected to the nearest face of the cube.

A UI was also added. Different scenes can now be selected to show a variety of features of the soft body system. It also shows the edge and volume compliance for the current scene. Camera movement was added through an open source script. Selection and movement of cubes was also added through another modified open source script. Through this, the user can interact with the soft body by pushing it with the cubes.