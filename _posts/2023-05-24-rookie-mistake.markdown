---
layout: post
title:  "Rookie mistake"
date:   2023-05-24
categories: jekyll update
---

In the last post I claimed something had gone wrong with the simulation, and energy wasn't being conserved. That turned out to be a rookie mistake. I was accidentally teleporting particles which hit the ground plane 1 unit up, making them gain a lot of velocity resulting in a bunny flying away from the screen. With that fixed I can now present some results:

<p style="text-align:center;">
    <img src="/soft-body-project/img/bun_reg.gif" alt="Regular bunny" width="32%">
    <img src="/soft-body-project/img/bun_squishy.gif" alt="Squishy bunny" width="32%">
    <img src="/soft-body-project/img/bun_firm.gif" alt="Firm bunny" width="32%">
    <i>Left: Regular bunny. Middle: Squishy bunny, high edge compliance, low volume compliance. Right: Firm bunny, low edge compliance, high volume compliance.</i>
</p>

In the above clips we can see that the result depends on the parameters of edge and volume compliance. Compliance is the inverse of stiffness, so a low compliance means a constraint is enforced strictly and a high compliance means it is applied loosely. The regular bunny sees some deformation. When the edge compliance is high there is no bounce and the bunny falls apart like a liquid, only being held together by volume constraints. Unexpectedly, for high volume compliance the simulation does not break even though the volume constraint is practically not being applied, only the edge constraints. In that case the bunny stays very firm.