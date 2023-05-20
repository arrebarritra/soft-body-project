---
layout: post
title:  "Bunny ascending"
date:   2023-05-20
categories: jekyll update
---

I have made quite a bit of progress on the project from last time, so todays post will have quite a few updates.

Firstly, I've switchede the soft body model from spring mass based to a position based method called [eXtended Position Based Dynamics (XPBD) by Müller et. al](https://matthias-research.github.io/pages/publications/XPBD.pdf). Additionally Müller also provides an excellent [video](https://youtu.be/uCaHXkS2cUg) in how to use the method for simulating soft bodies. Essentially, the method involves creating a tetrahedral mesh for the soft body you want to simulate, and running XPBD using various constraints. XPBD starts with an integration step where velocities and forces are updated to obtain an intermediate position for the vertices. Then, volume and edge length constraints are applied for the soft body to keep its shape. This is where XPBD really shines as the simulation does not easily break in contrast to the spring mass model with high stiffness. The volume constraints are applied proportional to and in the direction of the gradient, with a stiffness parameter also being included.

I have also had to learn a lot about compute shaders, which I've found to be a very interesting topic. Compute shaders are well suited to the problem at hand, as the calculations for integration and constraint application can be done in parallel for all the particles, tetrahedrons and edges. One of the challenges I will need to solve is multiple threads in the shader manipulating the position of particles. This will happen when applying constraints as a particle may be part of multiple edges or tetrahedrons.

The actual progress I've made on the project so far is designing a robust system around the soft body simulation, although the simulation itself is not yet implemented. I've set up all structs related to the simulation, filled compute buffers with initial particles positions and computed constraints that needs to be passed to the compute shader, made sure the threading works and set up the kernels in the shaders for XPBD. I've also made a class for the tetrahedral mesh to initialise data for the simulation and display the surface mesh. Finally, I did a little something to test the compute shader works:

![Bunny ascending](/soft-body-project/img/bunny_ascending.gif)